using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Romeo : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 37, 35);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 41, 39);

    private int CrossSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);
    private int AgonizeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private const int WeakAmount = 3;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/romeo/romeo.tscn";

    private static readonly LocString _mockBearAlive = L10NMonsterLookup("ROMEO.moves.MOCK.bearAlive");
    private static readonly LocString _mockBearDead = L10NMonsterLookup("ROMEO.moves.MOCK.bearDead");
    private static readonly LocString _deathReactLine = L10NMonsterLookup("ROMEO.deathReactLine");

    private const string CROSS_SLASH = "CROSS_SLASH";
    private const string MOCK = "MOCK";
    private const string AGONIZING_SLASH = "AGONIZING_SLASH";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        var bear = CombatState.GetTeammatesOf(Creature)
            .FirstOrDefault(t => t.Monster is Bear);
        if (bear != null)
        {
            bear.Died += BearDeathResponse;
        }
    }

    private void BearDeathResponse(Creature _)
    {
        _.Died -= BearDeathResponse;
        if (Creature.IsDead)
            return;
        TalkCmd.Play(_deathReactLine, Creature, VfxColor.Red, VfxDuration.Long);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var mockState = new MoveState(
            MOCK,
            Mock,
            new AbstractIntent[] { new UnknownIntent() }
        );

        var crossSlashState = new MoveState(
            CROSS_SLASH,
            CrossSlash,
            new AbstractIntent[] { new SingleAttackIntent(CrossSlashDamage) }
        );

        var agonizingSlashState = new MoveState(
            AGONIZING_SLASH,
            AgonizingSlash,
            new AbstractIntent[] { new SingleAttackIntent(AgonizeDamage), new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        mockState.FollowUpState = agonizingSlashState;
        agonizingSlashState.FollowUpState = moveBranch;
        crossSlashState.FollowUpState = moveBranch;

        states.Add(mockState);
        states.Add(crossSlashState);
        states.Add(agonizingSlashState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, mockState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        // A17+: Cross Slash up to twice in a row, then Agonizing Slash
        if (!LastTwoMoves(stateMachine, CROSS_SLASH))
        {
            return CROSS_SLASH;
        }
        return AGONIZING_SLASH;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Mock(IReadOnlyList<Creature> targets)
    {
        var bearAlive = CombatState.GetTeammatesOf(Creature)
            .Any(t => t != Creature && t.IsAlive && t.Monster is Bear);
        var line = bearAlive ? _mockBearAlive : _mockBearDead;
        TalkCmd.Play(line, Creature, VfxColor.Red,VfxDuration.Long);
    }

    private async Task CrossSlash(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Stab", 0.0f);
        await Cmd.Wait(0.5f);

        await DamageCmd.Attack(CrossSlashDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task AgonizingSlash(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Stab", 0.0f);
        await Cmd.Wait(0.5f);

        await DamageCmd.Attack(AgonizeDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, WeakAmount, Creature, null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Stab", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}