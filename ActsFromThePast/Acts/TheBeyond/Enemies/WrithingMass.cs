using ActsFromThePast.Cards;
using ActsFromThePast.Powers;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class WrithingMass : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);

    private int BigHitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 32);
    private int MultiHitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);
    private const int MultiHitCount = 3;
    private int AttackBlockDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
    private int AttackBlockBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
    private int AttackDebuffDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private const int NormalDebuffAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/writhing_mass/writhing_mass.tscn";

    private const string BIG_HIT = "BIG_HIT";
    private const string MULTI_HIT = "MULTI_HIT";
    private const string ATTACK_BLOCK = "ATTACK_BLOCK";
    private const string ATTACK_DEBUFF = "ATTACK_DEBUFF";
    private const string MEGA_DEBUFF = "MEGA_DEBUFF";

    private bool _firstMove = true;
    private bool _usedMegaDebuff = false;

    private bool FirstMove
    {
        get => _firstMove;
        set { AssertMutable(); _firstMove = value; }
    }

    private bool UsedMegaDebuff
    {
        get => _usedMegaDebuff;
        set { AssertMutable(); _usedMegaDebuff = value; }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ReactivePower>(Creature, 1, Creature, null);
        await PowerCmd.Apply<MalleablePower>(Creature, 3, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var bigHitState = new MoveState(
            BIG_HIT,
            BigHit,
            new AbstractIntent[] { new SingleAttackIntent(BigHitDamage) }
        );
        var multiHitState = new MoveState(
            MULTI_HIT,
            MultiHit,
            new AbstractIntent[] { new MultiAttackIntent(MultiHitDamage, MultiHitCount) }
        );
        var attackBlockState = new MoveState(
            ATTACK_BLOCK,
            AttackBlock,
            new AbstractIntent[] { new SingleAttackIntent(AttackBlockDamage), new DefendIntent() }
        );
        var attackDebuffState = new MoveState(
            ATTACK_DEBUFF,
            AttackDebuff,
            new AbstractIntent[] { new SingleAttackIntent(AttackDebuffDamage), new DebuffIntent() }
        );
        var megaDebuffState = new MoveState(
            MEGA_DEBUFF,
            MegaDebuff,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        bigHitState.FollowUpState = moveBranch;
        multiHitState.FollowUpState = moveBranch;
        attackBlockState.FollowUpState = moveBranch;
        attackDebuffState.FollowUpState = moveBranch;
        megaDebuffState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { bigHitState, multiHitState, attackBlockState, attackDebuffState, megaDebuffState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (FirstMove)
        {
            FirstMove = false;
            int firstRoll = rng.NextInt(100);
            if (firstRoll < 33) return MULTI_HIT;
            if (firstRoll < 66) return ATTACK_BLOCK;
            return ATTACK_DEBUFF;
        }

        int num = rng.NextInt(100);

        // 10%: Big Hit
        if (num < 10)
        {
            if (!LastMove(stateMachine, BIG_HIT))
                return BIG_HIT;
            // Reroll into 10-99 range (uniform over remaining buckets)
            num = 10 + rng.NextInt(90);
        }

        // 10%: Mega Debuff (num 10-19)
        if (num < 20)
        {
            if (!UsedMegaDebuff && !LastMove(stateMachine, MEGA_DEBUFF))
            {
                UsedMegaDebuff = true;
                return MEGA_DEBUFF;
            }
            // 10% chance to still do Big Hit, otherwise reroll into 20-99
            if (rng.NextFloat() < 0.1f && !LastMove(stateMachine, BIG_HIT))
                return BIG_HIT;
            num = 20 + rng.NextInt(80);
        }

        // 20%: Attack Debuff (num 20-39)
        if (num < 40)
        {
            if (!LastMove(stateMachine, ATTACK_DEBUFF))
                return ATTACK_DEBUFF;
            // 40% chance to reroll into 0-19, 60% into 40-99
            if (rng.NextFloat() < 0.4f)
            {
                // Reroll 0-19: Big Hit or Mega Debuff territory
                // Mega Debuff already used or blocked — fall through to Big Hit if available
                if (!LastMove(stateMachine, BIG_HIT))
                    return BIG_HIT;
            }
            num = 40 + rng.NextInt(60);
        }

        // 30%: Multi Hit (num 40-69)
        if (num < 70)
        {
            if (!LastMove(stateMachine, MULTI_HIT))
                return MULTI_HIT;
            // 30% chance for Attack Block, 70% reroll into 0-39
            if (rng.NextFloat() < 0.3f)
                return ATTACK_BLOCK;
            // Reroll into 0-39: prefer Attack Debuff if available
            if (!LastMove(stateMachine, ATTACK_DEBUFF))
                return ATTACK_DEBUFF;
            return BIG_HIT;
        }

        // 30%: Attack Block (num 70-99)
        if (!LastMove(stateMachine, ATTACK_BLOCK))
            return ATTACK_BLOCK;
        // Reroll into 0-69
        num = rng.NextInt(70);
        if (num < 10 && !LastMove(stateMachine, BIG_HIT))
            return BIG_HIT;
        if (num < 40 && !LastMove(stateMachine, ATTACK_DEBUFF))
            return ATTACK_DEBUFF;
        return MULTI_HIT;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task BigHit(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "BigSwing", 0.4f);
        await DamageCmd.Attack(BigHitDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task MultiHit(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        for (int i = 0; i < MultiHitCount; i++)
        {
            await DamageCmd.Attack(MultiHitDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
    }

    private async Task AttackBlock(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(AttackBlockDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.GainBlock(Creature, AttackBlockBlock, ValueProp.Move, null);
    }

    private async Task AttackDebuff(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(AttackDebuffDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, NormalDebuffAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(target, NormalDebuffAmount, Creature, null);
        }
    }

    private async Task MegaDebuff(IReadOnlyList<Creature> targets)
    {
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
        await Cmd.Wait(0.2f);
        var player = Creature.CombatState?.Players.FirstOrDefault();
        if (player != null)
            await CardPileCmd.AddCurseToDeck<Parasite>(player);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("BigSwing", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}