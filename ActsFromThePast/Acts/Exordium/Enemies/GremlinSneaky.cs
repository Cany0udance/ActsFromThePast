using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinSneaky : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

    private int PunctureDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_sneaky/gremlin_sneaky.tscn";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var punctureState = new MoveState(
            "PUNCTURE",
            Puncture,
            new AbstractIntent[] { new SingleAttackIntent(PunctureDamage) }
        );

        punctureState.FollowUpState = punctureState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { punctureState },
            punctureState
        );
    }

    private async Task Puncture(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(PunctureDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
        GremlinLeaderHelper.SubscribeToLeaderDeath(Creature, (CombatState)CombatState);
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "gremlin_sneaky_death_1",
            1 => "gremlin_sneaky_death_2",
            _ => "gremlin_sneaky_death_3"
        };
        ModAudio.Play("gremlin_sneaky", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("animation", true);
        var animator = new CreatureAnimator(idle, controller);

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        animState.Apply(controller.GetSkeleton());

        return animator;
    }
}