using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class SpireGrowth : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 170);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 170);

    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 25, 22);
    private int ConstrictAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spire_growth/spire_growth.tscn";

    private const string QUICK_TACKLE = "QUICK_TACKLE";
    private const string CONSTRICT = "CONSTRICT";
    private const string SMASH = "SMASH";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var quickTackleState = new MoveState(
            QUICK_TACKLE,
            QuickTackle,
            new SingleAttackIntent(TackleDamage)
        );

        var constrictState = new MoveState(
            CONSTRICT,
            Constrict,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var smashState = new MoveState(
            SMASH,
            Smash,
            new SingleAttackIntent(SmashDamage)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        quickTackleState.FollowUpState = moveBranch;
        constrictState.FollowUpState = moveBranch;
        smashState.FollowUpState = moveBranch;

        states.Add(quickTackleState);
        states.Add(constrictState);
        states.Add(smashState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var player = Creature.CombatState?.Players.FirstOrDefault();
        bool playerConstricted = player?.Creature?.Powers.Any(p => p is ConstrictedPower) ?? false;

        if (!playerConstricted && !LastMove(stateMachine, CONSTRICT))
            return CONSTRICT;

        int num = rng.NextInt(100);

        if (num < 50 && !LastTwoMoves(stateMachine, QUICK_TACKLE))
            return QUICK_TACKLE;

        if (!playerConstricted && !LastMove(stateMachine, CONSTRICT))
            return CONSTRICT;

        if (!LastTwoMoves(stateMachine, SMASH))
            return SMASH;

        return QUICK_TACKLE;
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

    private async Task QuickTackle(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(TackleDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Constrict(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<ConstrictedPower>(target, ConstrictAmount, Creature, null);
        }
    }

    private async Task Smash(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Smash", 0.0f);
        await Cmd.Wait(0.4f);

        await DamageCmd.Attack(SmashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hurt");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Smash", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(1.3f);

        return animator;
    }
}