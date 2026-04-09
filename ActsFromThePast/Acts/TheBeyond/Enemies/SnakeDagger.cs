using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class SnakeDagger : MonsterModel
{
    public override int MinInitialHp => 20;
    public override int MaxInitialHp => 25;

    private const int StabDamage = 9;
    private const int SacrificeDamage = 25;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/dagger/dagger.tscn";

    private const string WOUND_STAB = "WOUND_STAB";
    private const string EXPLODE = "EXPLODE";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var woundStab = new MoveState(
            WOUND_STAB,
            WoundStab,
            new AbstractIntent[] { new SingleAttackIntent(StabDamage), new StatusIntent(1) }
        );

        var explode = new MoveState(
            EXPLODE,
            Explode,
            new DeathBlowIntent((Func<Decimal>) (() => SacrificeDamage))
        );

        woundStab.FollowUpState = explode;
        explode.FollowUpState = explode;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { woundStab, explode },
            woundStab
        );
    }

    private async Task WoundStab(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Stab", 0.0f);
        await Cmd.Wait(0.3f);

        await DamageCmd.Attack(StabDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Discard, 1, false);
    }

    private async Task Explode(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Suicide", 0.0f);
        await Cmd.Wait(0.4f);

        await DamageCmd.Attack(SacrificeDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await CreatureCmd.Kill(Creature);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var stab = new AnimState("Attack");
        var suicide = new AnimState("Attack2");
        var hurt = new AnimState("Hurt");

        stab.NextState = idle;
        hurt.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Stab", stab);
        animator.AddAnyState("Suicide", suicide);
        animator.AddAnyState("Hurt", hurt);

        return animator;
    }
}