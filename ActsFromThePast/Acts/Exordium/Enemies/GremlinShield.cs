using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;
public sealed class GremlinShield : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 12);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 17, 15);

    private int ProtectBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 7);
    private int ShieldBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_shield/gremlin_shield.tscn";

    private const string PROTECT = "PROTECT";
    private const string SHIELD_BASH = "SHIELD_BASH";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
        GremlinLeaderHelper.SubscribeToLeaderDeath(Creature, CombatState);
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "gremlin_shield_death_1",
            _ => "gremlin_shield_death_2"
        };
        ModAudio.Play("gremlin_shield", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var protectState = new MoveState(
            PROTECT,
            Protect,
            new AbstractIntent[] { new DefendIntent() }
        );

        var shieldBashState = new MoveState(
            SHIELD_BASH,
            ShieldBash,
            new AbstractIntent[] { new SingleAttackIntent(ShieldBashDamage) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        protectState.FollowUpState = moveBranch;
        shieldBashState.FollowUpState = shieldBashState;

        states.Add(protectState);
        states.Add(shieldBashState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, protectState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var teammateCount = CombatState.GetTeammatesOf(Creature).Count;
        return teammateCount > 1 ? PROTECT : SHIELD_BASH;
    }

    private async Task Protect(IReadOnlyList<Creature> targets)
    {
        var teammates = CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive);
        var target = teammates.Any() ? Rng.NextItem(teammates) : Creature;

        await CreatureCmd.GainBlock(target, ProtectBlock, ValueProp.Move, null);
    }

    private async Task ShieldBash(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(ShieldBashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var animator = new CreatureAnimator(idle, controller);

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        animState.Apply(controller.GetSkeleton());

        return animator;
    }
}