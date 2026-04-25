using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class AcidSlimeMedium : CustomMonsterModel
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

    public override int MinInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 29, 28);
    public override int MaxInitialHp => OverrideHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 32);

    private int CorrosiveSpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private const int WeakTurns = 1;
    private const int SlimedCount = 1;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/acid_slime_medium/acid_slime_medium.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string CORROSIVE_SPIT = "CORROSIVE_SPIT";
    private const string TACKLE = "TACKLE";
    private const string LICK = "LICK";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_die");
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

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        corrosiveSpitState.FollowUpState = moveBranch;
        tackleState.FollowUpState = moveBranch;
        lickState.FollowUpState = moveBranch;

        states.Add(corrosiveSpitState);
        states.Add(tackleState);
        states.Add(lickState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 40)
        {
            // 40% Corrosive Spit, unless last two were Corrosive Spit
            if (LastTwoMoves(stateMachine, CORROSIVE_SPIT))
            {
                return rng.NextFloat() < 0.5f ? TACKLE : LICK;
            }
            return CORROSIVE_SPIT;
        }
        else if (num < 80)
        {
            // 40% Tackle, unless last two were Tackle
            if (LastTwoMoves(stateMachine, TACKLE))
            {
                return rng.NextFloat() < 0.5f ? CORROSIVE_SPIT : LICK;
            }
            return TACKLE;
        }
        else
        {
            // 20% Lick, unless last was Lick
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
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);

        try
        {
            ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, (Player)null);
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
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, WeakTurns, Creature, null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var damage = new AnimState("damage");
    
        damage.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", damage);
    
        return animator;
    }
}