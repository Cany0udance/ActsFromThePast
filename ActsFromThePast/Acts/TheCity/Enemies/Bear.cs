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
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Bear : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

    private int MaulDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);
    private const int LungeBlock = 9;
    private int DexReduction => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/bear/bear.tscn";

    private const string MAUL = "MAUL";
    private const string BEAR_HUG = "BEAR_HUG";
    private const string LUNGE = "LUNGE";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var bearHugState = new MoveState(
            BEAR_HUG,
            BearHug,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var maulState = new MoveState(
            MAUL,
            Maul,
            new AbstractIntent[] { new SingleAttackIntent(MaulDamage) }
        );

        var lungeState = new MoveState(
            LUNGE,
            Lunge,
            new AbstractIntent[] { new SingleAttackIntent(LungeDamage), new DefendIntent() }
        );

        bearHugState.FollowUpState = lungeState;
        lungeState.FollowUpState = maulState;
        maulState.FollowUpState = lungeState;

        states.Add(bearHugState);
        states.Add(maulState);
        states.Add(lungeState);

        return new MonsterMoveStateMachine(states, bearHugState);
    }

    private async Task BearHug(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<DexterityPower>(target, -DexReduction, Creature, null);
        }
    }

    private async Task Maul(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Maul", 0.0f);
        await Cmd.Wait(0.3f);

        await DamageCmd.Attack(MaulDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Lunge(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(LungeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        await CreatureCmd.GainBlock(Creature, LungeBlock, ValueProp.Move, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Maul", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}