using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast;

public sealed class Pointy : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 30);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 30);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private const int AttackHits = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/pointy/pointy.tscn";

    private static readonly LocString _deathReactLine = L10NMonsterLookup("POINTY.deathReactLine");

    private const string STAB = "STAB";

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
        TalkCmd.Play(_deathReactLine, Creature, 2.0);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var stabState = new MoveState(
            STAB,
            Stab,
            new AbstractIntent[] { new MultiAttackIntent(AttackDamage, AttackHits) }
        );

        stabState.FollowUpState = stabState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { stabState },
            stabState
        );
    }

    private async Task Stab(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Slash", 0.0f);
        await Cmd.Wait(0.4f);

        for (int i = 0; i < AttackHits; i++)
        {
            await DamageCmd.Attack(AttackDamage)
                .FromMonster(this)
                .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
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
        animator.AddAnyState("Slash", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}