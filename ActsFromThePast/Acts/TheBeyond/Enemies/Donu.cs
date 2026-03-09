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

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Donu : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250);

    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private const int BeamCount = 2;
    private const int CircleStrengthAmount = 3;
    private int ArtifactAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/donu/donu.tscn";

    private const string BEAM = "BEAM";
    private const string CIRCLE_OF_PROTECTION = "CIRCLE_OF_PROTECTION";

    private bool _isAttacking;

    private bool IsAttacking
    {
        get => _isAttacking;
        set
        {
            AssertMutable();
            _isAttacking = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(Creature, ArtifactAmount, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var circleState = new MoveState(
            CIRCLE_OF_PROTECTION,
            CircleOfProtection,
            new BuffIntent()
        );

        var beamState = new MoveState(
            BEAM,
            Beam,
            new MultiAttackIntent(BeamDamage, BeamCount)
        );

        circleState.FollowUpState = beamState;
        beamState.FollowUpState = circleState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { circleState, beamState },
            circleState
        );
    }

    private async Task CircleOfProtection(IReadOnlyList<Creature> targets)
    {
        ModAudio.Play("donu", "donu_defense");

        var teammates = CombatState.GetTeammatesOf(Creature);
        foreach (var teammate in teammates)
        {
            if (teammate.IsAlive)
            {
                await PowerCmd.Apply<StrengthPower>(teammate, (decimal)CircleStrengthAmount, Creature, (CardModel)null);
            }
        }
    }

    private async Task Beam(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Beam", 0.0f);
        await Cmd.Wait(0.5f);

        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .WithHitCount(BeamCount)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack_2");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Beam", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}