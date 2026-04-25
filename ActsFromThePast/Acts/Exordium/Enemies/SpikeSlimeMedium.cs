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

public sealed class SpikeSlimeMedium : CustomMonsterModel
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

    private int FlameTackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private const int FrailTurns = 1;
    private const int SlimedCount = 1;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spike_slime_medium/spike_slime_medium.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string FLAME_TACKLE = "FLAME_TACKLE";
    private const string LICK = "LICK";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/leaf_slime_m/leaf_slime_m_die");
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

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        flameTackleState.FollowUpState = moveBranch;
        lickState.FollowUpState = moveBranch;

        states.Add(flameTackleState);
        states.Add(lickState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 30)
        {
            // 30% Flame Tackle, unless last two were Flame Tackle
            if (LastTwoMoves(stateMachine, FLAME_TACKLE))
            {
                return LICK;
            }
            return FLAME_TACKLE;
        }
        else
        {
            // 70% Lick, unless last was Lick
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
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/leaf_slime_m/leaf_slime_m_attack")
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

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, FrailTurns, Creature, null);
        }
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