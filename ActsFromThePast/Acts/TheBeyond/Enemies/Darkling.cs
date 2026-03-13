using ActsFromThePast.Powers;
using Godot;
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
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Darkling : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 59, 56);

    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private const int ChompHits = 2;
    private const int HardenBlock = 12;
    private int HardenStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 0);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/darkling/darkling.tscn";

    private const string CHOMP = "CHOMP";
    private const string HARDEN = "HARDEN";
    private const string NIP = "NIP";
    private const string DEAD_MOVE = "DEAD_MOVE";
    private const string REATTACH_MOVE = "REATTACH_MOVE";

    private int? _nipDamage;
    private bool _firstMove = true;
    private int _slotIndex;
    private MoveState _deadState;

    private int NipDamage => _nipDamage ??= AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies)
        ? GD.RandRange(9, 13)
        : GD.RandRange(7, 11);

    public bool FirstMove
    {
        get => _firstMove;
        set
        {
            AssertMutable();
            _firstMove = value;
        }
    }

    public int SlotIndex
    {
        get => _slotIndex;
        set
        {
            AssertMutable();
            _slotIndex = value;
        }
    }

    public MoveState DeadState
    {
        get => _deadState;
        private set
        {
            AssertMutable();
            _deadState = value;
        }
    }

    public override bool ShouldFadeAfterDeath => false;
    public override bool ShouldDisappearFromDoom => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        var nipMin = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);
        var nipMax = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);
        _nipDamage = Rng.Chaotic.NextInt(nipMax - nipMin + 1) + nipMin;

        int healAmount = Creature.MaxHp / 2;
        await PowerCmd.Apply<LifeLinkPower>(Creature, healAmount, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var hardenIntents = HardenStrength > 0
            ? new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
            : new AbstractIntent[] { new DefendIntent() };

        var chompState = new MoveState(
            CHOMP,
            Chomp,
            new MultiAttackIntent(ChompDamage, ChompHits)
        );

        var hardenState = new MoveState(
            HARDEN,
            Harden,
            hardenIntents
        );

        var nipState = new MoveState(
            NIP,
            Nip,
            new SingleAttackIntent(NipDamage)
        );

        DeadState = new MoveState(
            DEAD_MOVE,
            DeadMove,
            Array.Empty<AbstractIntent>()
        );

        var reattachState = new MoveState(
            REATTACH_MOVE,
            ReattachMove,
            new AbstractIntent[] { new HealIntent() }
        )
        {
            MustPerformOnceBeforeTransitioning = true
        };

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        chompState.FollowUpState = moveBranch;
        hardenState.FollowUpState = moveBranch;
        nipState.FollowUpState = moveBranch;

        DeadState.FollowUpState = reattachState;
        reattachState.FollowUpState = moveBranch;

        states.Add(chompState);
        states.Add(hardenState);
        states.Add(nipState);
        states.Add(DeadState);
        states.Add(reattachState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (FirstMove)
        {
            FirstMove = false;
            int num = rng.NextInt(100);
            return num < 50 ? HARDEN : NIP;
        }

        int roll = rng.NextInt(100);

        if (roll < 40)
        {
            if (!LastMove(stateMachine, CHOMP) && SlotIndex % 2 == 0)
                return CHOMP;
            else
                return SelectNextMove(owner, rng, stateMachine, rng.NextInt(60) + 40);
        }
        else if (roll < 70)
        {
            if (!LastMove(stateMachine, HARDEN))
                return HARDEN;
            else
                return NIP;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, NIP))
                return NIP;
            else
                return SelectNextMove(owner, rng, stateMachine, rng.NextInt(100));
        }
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine, int forcedRoll)
    {
        if (forcedRoll < 40)
        {
            if (!LastMove(stateMachine, CHOMP) && SlotIndex % 2 == 0)
                return CHOMP;
            return forcedRoll < 20 ? HARDEN : NIP;
        }
        else if (forcedRoll < 70)
        {
            if (!LastMove(stateMachine, HARDEN))
                return HARDEN;
            return NIP;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, NIP))
                return NIP;
            return !LastMove(stateMachine, HARDEN) ? HARDEN : CHOMP;
        }
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Chomp(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Chomp", 0.0f);
        await Cmd.Wait(0.5f);

        await DamageCmd.Attack(ChompDamage)
            .WithHitCount(ChompHits)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Harden(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, HardenBlock, ValueProp.Move, null);

        if (HardenStrength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(Creature, HardenStrength, Creature, null);
        }
    }

    private async Task Nip(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(NipDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private Task DeadMove(IReadOnlyList<Creature> targets) => Task.CompletedTask;

    private async Task ReattachMove(IReadOnlyList<Creature> targets)
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll == 0 ? "darkling_regrow_1" : "darkling_regrow_2";
        ModAudio.Play("darkling", sfxName);

        var regrowPower = Creature.Powers.OfType<LifeLinkPower>().FirstOrDefault();
        if (regrowPower != null)
        {
            await regrowPower.DoReattach();
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");
        var dead = new AnimState("dead_loop", true);
        var wither = new AnimState("wither");
        var regenerate = new AnimState("regenerate");

        attack.NextState = idle;
        hit.NextState = idle;
        wither.NextState = dead;
        regenerate.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Chomp", attack);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", wither);
        animator.AddAnyState("Revive", regenerate);
        controller.GetAnimationState().SetTimeScale(Rng.Chaotic.NextFloat(0.75f, 1.0f));

        return animator;
    }
}