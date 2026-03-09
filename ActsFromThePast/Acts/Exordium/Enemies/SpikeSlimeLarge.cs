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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SpikeSlimeLarge : MonsterModel
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
    public override int MinInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 67, 64);
    public override int MaxInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 73, 70);

    private int FlameTackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
    private int FrailTurns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private const int SlimedCount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spike_slime_large/spike_slime_large.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string FLAME_TACKLE = "FLAME_TACKLE";
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

        var flameTackleState = new MoveState(
            FLAME_TACKLE,
            FlameTackle,
            new AbstractIntent[] { new SingleAttackIntent(FlameTackleDamage), new StatusIntent(SlimedCount) }
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

        flameTackleState.FollowUpState = moveBranch;
        lickState.FollowUpState = moveBranch;
        _splitState.FollowUpState = _splitState;

        states.Add(flameTackleState);
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

        if (num < 30)
        {
            if (LastTwoMoves(stateMachine, FLAME_TACKLE))
            {
                return LICK;
            }
            return FLAME_TACKLE;
        }
        else
        {
            if (LastMove(stateMachine, LICK))
            {
                return FLAME_TACKLE;
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

    private async Task FlameTackle(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(FlameTackleDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);

        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, false);
    }

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(target, FrailTurns, Creature, null);
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
    
        var slime1 = (SpikeSlimeMedium)ModelDb.Monster<SpikeSlimeMedium>().ToMutable();
        slime1.OverrideHp = currentHp;
        var creature1 = await CreatureCmd.Add(slime1, combatState, CombatSide.Enemy, null);
        var node1 = NCombatRoom.Instance?.GetCreatureNode(creature1);
        if (node1 != null)
            node1.Position = originalPosition + new Vector2(-134f, Rng.Chaotic.NextFloat() * 8f - 4f);
    
        var slime2 = (SpikeSlimeMedium)ModelDb.Monster<SpikeSlimeMedium>().ToMutable();
        slime2.OverrideHp = currentHp;
        var creature2 = await CreatureCmd.Add(slime2, combatState, CombatSide.Enemy, null);
        var node2 = NCombatRoom.Instance?.GetCreatureNode(creature2);
        if (node2 != null)
            node2.Position = originalPosition + new Vector2(134f, Rng.Chaotic.NextFloat() * 8f - 4f);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var hit = new AnimState("hit");
    
        hit.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", hit);
    
        return animator;
    }
}