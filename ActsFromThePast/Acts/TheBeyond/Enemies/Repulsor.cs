using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Repulsor : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 31, 29);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 38, 35);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);
    private const int DazeAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/repulsor/repulsor.tscn";

    private const string ATTACK = "ATTACK";
    private const string DAZE = "DAZE";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var attackState = new MoveState(
            ATTACK,
            Attack,
            new SingleAttackIntent(AttackDamage)
        );

        var dazeState = new MoveState(
            DAZE,
            Daze,
            new StatusIntent(DazeAmount)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        attackState.FollowUpState = moveBranch;
        dazeState.FollowUpState = moveBranch;

        states.Add(attackState);
        states.Add(dazeState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 20 && !LastMove(stateMachine, ATTACK))
        {
            return ATTACK;
        }

        return DAZE;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task Attack(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task Daze(IReadOnlyList<Creature> targets)
    {
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, DazeAmount, false);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}