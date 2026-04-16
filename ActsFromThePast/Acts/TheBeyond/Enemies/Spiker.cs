using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Spiker : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 56);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);
    private int StartingThorns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 4);
    private const int BuffAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spiker/spiker.tscn";

    private const string ATTACK = "ATTACK";
    private const string BUFF_THORNS = "BUFF_THORNS";

    private int _thornsCount;

    private int ThornsCount
    {
        get => _thornsCount;
        set
        {
            AssertMutable();
            _thornsCount = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _thornsCount = 0;
        await PowerCmd.Apply<ThornsPower>(Creature, StartingThorns, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var attackState = new MoveState(
            ATTACK,
            Attack,
            new SingleAttackIntent(AttackDamage)
        );

        var buffThornsState = new MoveState(
            BUFF_THORNS,
            BuffThorns,
            new BuffIntent()
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        attackState.FollowUpState = moveBranch;
        buffThornsState.FollowUpState = moveBranch;

        states.Add(attackState);
        states.Add(buffThornsState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (ThornsCount > 5)
        {
            return ATTACK;
        }

        int num = rng.NextInt(100);

        if (num < 50 && !LastMove(stateMachine, ATTACK))
        {
            return ATTACK;
        }

        return BUFF_THORNS;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task Attack(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "SpikerPounce", 0.0f);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var trackEntry = animState.GetCurrent(0);
            if (trackEntry != null)
                trackEntry.SetTimeScale(3.0f);
        }

        await Cmd.Wait(0.25f);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task BuffThorns(IReadOnlyList<Creature> targets)
    {
        ThornsCount++;
        await PowerCmd.Apply<ThornsPower>(Creature, BuffAmount, Creature, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // TODO look into speeding up the hit animation
        var idle = new AnimState("idle", true);
        var pounce = new AnimState("attack");
        var hit = new AnimState("damaged");

        pounce.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("SpikerPounce", pounce);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}