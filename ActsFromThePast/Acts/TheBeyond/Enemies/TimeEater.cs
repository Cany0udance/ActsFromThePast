using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class TimeEater : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);

    private int ReverbDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private const int ReverbHits = 3;
    private int HeadSlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 26);
    private const int RippleBlock = 20;
    private const int DebuffTurns = 1;
    private const int SlimedCount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/time_eater/time_eater.tscn";

    private const string REVERBERATE = "REVERBERATE";
    private const string RIPPLE = "RIPPLE";
    private const string HEAD_SLAM = "HEAD_SLAM";
    private const string HASTE = "HASTE";

    private static readonly LocString _hasteDialog = L10NMonsterLookup("TIME_EATER.banter.haste");
    private static readonly LocString _introDialog = L10NMonsterLookup("TIME_EATER.banter.intro");

    private bool _usedHaste;
    private bool _firstTurn = true;

    private bool UsedHaste
    {
        get => _usedHaste;
        set { AssertMutable(); _usedHaste = value; }
    }

    private bool FirstTurn
    {
        get => _firstTurn;
        set { AssertMutable(); _firstTurn = value; }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<TimeWarpPower>(Creature, 1M, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var reverberateState = new MoveState(
            REVERBERATE,
            Reverberate,
            new AbstractIntent[] { new MultiAttackIntent(ReverbDamage, ReverbHits) }
        );
        var rippleState = new MoveState(
            RIPPLE,
            Ripple,
            new AbstractIntent[] { new DefendIntent(), new DebuffIntent() }
        );
        var headSlamState = new MoveState(
            HEAD_SLAM,
            HeadSlam,
            new AbstractIntent[] { new SingleAttackIntent(HeadSlamDamage), new DebuffIntent(), new StatusIntent(SlimedCount) }
        );
        var hasteState = new MoveState(
            HASTE,
            Haste,
            new AbstractIntent[] { new BuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        reverberateState.FollowUpState = moveBranch;
        rippleState.FollowUpState = moveBranch;
        headSlamState.FollowUpState = moveBranch;
        hasteState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { reverberateState, rippleState, headSlamState, hasteState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (owner.CurrentHp < owner.MaxHp / 2 && !UsedHaste)
        {
            UsedHaste = true;
            return HASTE;
        }

        int num = rng.NextInt(100);

        if (num < 45)
        {
            if (!LastTwoMoves(stateMachine, REVERBERATE))
                return REVERBERATE;
            // Reroll into 50-99
            num = 50 + rng.NextInt(50);
        }

        if (num < 80)
        {
            if (!LastMove(stateMachine, HEAD_SLAM))
                return HEAD_SLAM;
            return rng.NextFloat() < 0.66f ? REVERBERATE : RIPPLE;
        }
        else
        {
            if (!LastMove(stateMachine, RIPPLE))
                return RIPPLE;
            // Reroll into 0-74
            num = rng.NextInt(75);
            if (num < 45)
            {
                if (!LastTwoMoves(stateMachine, REVERBERATE))
                    return REVERBERATE;
            }
            if (!LastMove(stateMachine, HEAD_SLAM))
                return HEAD_SLAM;
            return rng.NextFloat() < 0.66f ? REVERBERATE : RIPPLE;
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

    private async Task PlayIntroIfFirstTurn()
    {
        if (!FirstTurn)
            return;
        FirstTurn = false;
        TalkCmd.Play(_introDialog, Creature, 3.0);
        await Cmd.Wait(0.5f);
    }

    private async Task Reverberate(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        for (int i = 0; i < ReverbHits; i++)
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode != null)
                ShockWaveEffect.PlayRoyal(creatureNode.VfxSpawnPosition);
            await Cmd.Wait(0.75f);
            await DamageCmd.Attack(ReverbDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
    }

    private async Task Ripple(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        await CreatureCmd.GainBlock(Creature, RippleBlock, ValueProp.Move, null);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(target, DebuffTurns, Creature, null);
            await PowerCmd.Apply<WeakPower>(target, DebuffTurns, Creature, null);
            await PowerCmd.Apply<FrailPower>(target, DebuffTurns, Creature, null);
        }
    }

    private async Task HeadSlam(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        await CreatureCmd.TriggerAnim(Creature, "Slam", 0.4f);
        await DamageCmd.Attack(HeadSlamDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<DrawReductionPower>(target, 1, Creature, null);
        }
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, false);
    }

    private async Task Haste(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        TalkCmd.Play(_hasteDialog, Creature, 3.0);

        var debuffs = Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        foreach (var debuff in debuffs)
            await PowerCmd.Remove(debuff);

        int healAmount = Creature.MaxHp / 2 - Creature.CurrentHp;
        if (healAmount > 0)
            await CreatureCmd.Heal(Creature, healAmount);

        await CreatureCmd.GainBlock(Creature, HeadSlamDamage, ValueProp.Move, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Slam", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}