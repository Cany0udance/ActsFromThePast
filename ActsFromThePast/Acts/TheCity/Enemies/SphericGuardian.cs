using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SphericGuardian : CustomMonsterModel
{
    public override int MinInitialHp => 20;
    public override int MaxInitialHp => 20;

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int ActivateBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 35, 25);
    private const int SlamHits = 2;
    private const int HardenBlock = 15;
    private const int FrailAmount = 5;
    private const int ArtifactAmount = 3;
    private const int StartingBlock = 40;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/spheric_guardian/spheric_guardian.tscn";

    private const string ACTIVATE = "ACTIVATE";
    private const string FRAIL_ATTACK = "FRAIL_ATTACK";
    private const string SLAM = "SLAM";
    private const string HARDEN = "HARDEN";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<BarricadePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, ArtifactAmount, Creature, null);
    
        var blockProp = typeof(Creature).GetProperty("Block");
        var setter = blockProp?.GetSetMethod(true);
        
        setter?.Invoke(Creature, new object[] { (int)StartingBlock });
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);

        if (creature != Creature)
            return;

        PlayDetectSfx();
    }

    private void PlayDetectSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "spheric_guardian_talk_1",
            _ => "spheric_guardian_talk_2"
        };
        ModAudio.Play("spheric_guardian", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var activateState = new MoveState(
            ACTIVATE,
            ActivateMove,
            new AbstractIntent[] { new DefendIntent() }
        );

        var frailAttackState = new MoveState(
            FRAIL_ATTACK,
            FrailAttackMove,
            new AbstractIntent[] { new SingleAttackIntent(AttackDamage), new DebuffIntent() }
        );

        var slamState = new MoveState(
            SLAM,
            SlamMove,
            new AbstractIntent[] { new MultiAttackIntent(AttackDamage, SlamHits) }
        );

        var hardenState = new MoveState(
            HARDEN,
            HardenMove,
            new AbstractIntent[] { new SingleAttackIntent(AttackDamage), new DefendIntent() }
        );

        activateState.FollowUpState = frailAttackState;
        frailAttackState.FollowUpState = slamState;
        slamState.FollowUpState = hardenState;
        hardenState.FollowUpState = slamState;

        states.Add(activateState);
        states.Add(frailAttackState);
        states.Add(slamState);
        states.Add(hardenState);

        return new MonsterMoveStateMachine(states, activateState);
    }

    private async Task ActivateMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, ActivateBlock, ValueProp.Move, null);
        await Cmd.Wait(0.2f);
        PlayDetectSfx();
    }

    private async Task FrailAttackMove(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
        }
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Slam", 0.0f);
        await Cmd.Wait(0.4f);

        for (int i = 0; i < SlamHits; i++)
        {
            await DamageCmd.Attack(AttackDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
    }

    private async Task HardenMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, HardenBlock, ValueProp.Move, null);

        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var slam = new AnimState("Attack");
        var hit = new AnimState("Hit");

        slam.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Slam", slam);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}