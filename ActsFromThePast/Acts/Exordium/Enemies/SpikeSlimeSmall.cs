using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ActsFromThePast;

public sealed class SpikeSlimeSmall : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spike_slime_small/spike_slime_small.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string TACKLE = "TACKLE";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/leaf_slime_s/leaf_slime_s_die");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackleState = new MoveState(
            TACKLE,
            Tackle,
            new AbstractIntent[] { new SingleAttackIntent(TackleDamage) }
        );

        tackleState.FollowUpState = tackleState;

        states.Add(tackleState);

        return new MonsterMoveStateMachine(states, tackleState);
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(TackleDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var hit = new AnimState("hit");
    
        hit.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", hit);
    
        return animator;
    }
}