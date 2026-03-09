using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ActsFromThePast;

public sealed class AcidSlimeSmall : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 9, 8);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 12);

    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private const int WeakTurns = 1;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/acid_slime_small/acid_slime_small.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    private const string TACKLE = "TACKLE";
    private const string LICK = "LICK";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        NAudioManager.Instance.PlayOneShot("event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_die");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackleState = new MoveState(
            TACKLE,
            Tackle,
            new AbstractIntent[] { new SingleAttackIntent(TackleDamage) }
        );

        var lickState = new MoveState(
            LICK,
            Lick,
            new AbstractIntent[] { new DebuffIntent() }
        );

        tackleState.FollowUpState = lickState;
        lickState.FollowUpState = tackleState;

        states.Add(tackleState);
        states.Add(lickState);

        return new MonsterMoveStateMachine(states, lickState);
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

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, WeakTurns, Creature, null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var damage = new AnimState("damage");
    
        damage.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", damage);
    
        return animator;
    }
}