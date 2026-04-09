using ActsFromThePast.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class AcidSlimeLarge : MonsterModel
{
    private int? _overrideHp;

    public int? OverrideHp
    {
        get => _overrideHp;
        set
        {
            AssertMutable();
            _overrideHp = value;
        }
    }
    public override int MinInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 68, 65);
    public override int MaxInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 72, 69);

    private int CorrosiveSpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);
    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
    private const int WeakTurns = 2;
    private const int SlimedCount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/acid_slime_large/acid_slime_large.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string CORROSIVE_SPIT = "CORROSIVE_SPIT";
    private const string TACKLE = "TACKLE";
    private const string LICK = "LICK";
    private const string SPLIT = "SPLIT";

    private bool _splitTriggered;
    public bool SplitTriggered
    {
        get => _splitTriggered;
        set
        {
            AssertMutable();
            _splitTriggered = value;
        }
    }

    private MoveState _splitState;
    public MoveState SplitState => _splitState;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SplitPower>(Creature, 1m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var corrosiveSpitState = new MoveState(
            CORROSIVE_SPIT,
            CorrosiveSpit,
            new AbstractIntent[] { new SingleAttackIntent(CorrosiveSpitDamage), new StatusIntent(SlimedCount) }
        );

        var tackleState = new MoveState(
            TACKLE,
            Tackle,
            new AbstractIntent[] { new SingleAttackIntent(TackleDamage) }
        );

        var lickState = new MoveState(
            LICK,
            Lick,
            new AbstractIntent[] { new DebuffIntent() }
        );

        _splitState = new MoveState(
            SPLIT,
            Split,
            new AbstractIntent[] { new UnknownIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        corrosiveSpitState.FollowUpState = moveBranch;
        tackleState.FollowUpState = moveBranch;
        lickState.FollowUpState = moveBranch;
        _splitState.FollowUpState = _splitState;

        states.Add(corrosiveSpitState);
        states.Add(tackleState);
        states.Add(lickState);
        states.Add(_splitState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (SplitTriggered)
        {
            return SPLIT;
        }

        int num = rng.NextInt(100);

        if (num < 40)
        {
            if (LastTwoMoves(stateMachine, CORROSIVE_SPIT))
            {
                return rng.NextFloat() < 0.6f ? TACKLE : LICK;
            }
            return CORROSIVE_SPIT;
        }
        else if (num < 70)
        {
            if (LastTwoMoves(stateMachine, TACKLE))
            {
                return rng.NextFloat() < 0.6f ? CORROSIVE_SPIT : LICK;
            }
            return TACKLE;
        }
        else
        {
            if (LastMove(stateMachine, LICK))
            {
                return rng.NextFloat() < 0.4f ? CORROSIVE_SPIT : TACKLE;
            }
            return LICK;
        }
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task CorrosiveSpit(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(CorrosiveSpitDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);

        try
        {
            ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, false);
        }
        finally
        {
            ClassicSlimedTracker.CreatingClassicSlimed = false;
        }
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(TackleDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, WeakTurns, Creature, null);
        }
    }

    private async Task Split(IReadOnlyList<Creature> targets)
    {
        var currentHp = Creature.CurrentHp;
        var combatState = Creature.CombatState;
        var originalPosition = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Position ?? Vector2.Zero;

        _ = ShakeAnimation.Play(Creature, 1.0f, 3.0f);
        await Cmd.Wait(1.0f);
        ModAudio.Play("general", "slime_split");
        await CreatureCmd.Kill(Creature);

        var positionQueue = new Queue<Vector2>();
        var enemyContainer = NCombatRoom.Instance?.GetNode<Godot.Control>("%EnemyContainer");
        void OnChildEntered(Node child)
        {
            if (child is NCreature nc && positionQueue.Count > 0)
                nc.Position = positionQueue.Dequeue();
        }
        enemyContainer?.Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(OnChildEntered));

        positionQueue.Enqueue(originalPosition + new Vector2(-134f, Rng.Chaotic.NextFloat() * 8f - 4f));
        var slime1 = (AcidSlimeMedium)ModelDb.Monster<AcidSlimeMedium>().ToMutable();
        var creature1 = await CreatureCmd.Add(slime1, combatState, CombatSide.Enemy, null);
        await CreatureCmd.SetMaxHp(creature1, currentHp);
        await CreatureCmd.Heal(creature1, currentHp);

        positionQueue.Enqueue(originalPosition + new Vector2(134f, Rng.Chaotic.NextFloat() * 8f - 4f));
        var slime2 = (AcidSlimeMedium)ModelDb.Monster<AcidSlimeMedium>().ToMutable();
        var creature2 = await CreatureCmd.Add(slime2, combatState, CombatSide.Enemy, null);
        await CreatureCmd.SetMaxHp(creature2, currentHp);
        await CreatureCmd.Heal(creature2, currentHp);

        enemyContainer?.Disconnect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(OnChildEntered));
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var damage = new AnimState("damage");
    
        damage.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", damage);
    
        return animator;
    }
}