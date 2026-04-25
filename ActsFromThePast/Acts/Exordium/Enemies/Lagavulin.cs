using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Lagavulin : CustomMonsterModel
{
    public const string SLEEP = "SLEEP";
    public const string ATTACK = "ATTACK";
    public const string DEBUFF = "DEBUFF";

    private static readonly LocString _sleepLine1 = L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.SLEEP_1");
    private static readonly LocString _sleepLine2 = L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.SLEEP_2");
    private static readonly LocString _wakeLine = L10NMonsterLookup("ACTSFROMTHEPAST-LAGAVULIN.dialog.WAKE");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 112, 109);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 115, 111);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int DebuffAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, -2, -1);
    private const int MetalAmount = 8;
    private const int AsleepTurns = 3;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/lagavulin/lagavulin.tscn";
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

    private bool _isAwake;
    private int _debuffTurnCount;
    private int _sleepTurnCount;
    private NSleepingVfx? _sleepingVfx;
    private bool _startsAwake;

    public bool IsAwake
    {
        get => _isAwake;
        set
        {
            AssertMutable();
            _isAwake = value;
        }
    }

    public int DebuffTurnCount
    {
        get => _debuffTurnCount;
        set
        {
            AssertMutable();
            _debuffTurnCount = value;
        }
    }

    public int SleepTurnCount
    {
        get => _sleepTurnCount;
        set
        {
            AssertMutable();
            _sleepTurnCount = value;
        }
    }

    public bool StartsAwake
    {
        get => _startsAwake;
        set
        {
            AssertMutable();
            _startsAwake = value;
        }
    }

    private NSleepingVfx? SleepingVfx
    {
        get => _sleepingVfx;
        set
        {
            AssertMutable();
            _sleepingVfx = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
    
        if (StartsAwake)
        {
            IsAwake = true;
            ApplyAwakeBounds();
        }
        else
        {
            await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), Creature, MetalAmount, Creature, null);
            await PowerCmd.Apply<AsleepLagavulinPower>(new ThrowingPlayerChoiceContext(), Creature, AsleepTurns, Creature, null);
        
            var specialNode = NCombatRoom.Instance?.GetCreatureNode(Creature)?.GetSpecialNode<Marker2D>("%SleepVfxPos");
            if (specialNode != null)
            {
                SleepingVfx = NSleepingVfx.Create(specialNode.GlobalPosition);
                specialNode.AddChildSafely(SleepingVfx);
                SleepingVfx.Position = Vector2.Zero;
            }
        }
    
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        StopSleepingVfx();
    }

    public void StopSleepingVfx()
    {
        SleepingVfx?.Stop();
        SleepingVfx = null;
    }

    public async Task WakeUpFromDamage()
    {
        TalkCmd.Play(_wakeLine, Creature, VfxColor.White, VfxDuration.Long);
        await CreatureCmd.TriggerAnim(Creature, "Wake", 0.6f);
        StopSleepingVfx();
        IsAwake = true;
        ApplyAwakeBounds();
        await CreatureCmd.Stun(Creature, StunnedMove, ATTACK);
        NRunMusicController.Instance?.UpdateTrack();
    }

    public async Task WakeUpNaturally()
    {
        TalkCmd.Play(_wakeLine, Creature, VfxColor.White, VfxDuration.Long);
        await CreatureCmd.TriggerAnim(Creature, "Wake", 0.6f);
        StopSleepingVfx();
        IsAwake = true;
    ApplyAwakeBounds();
        NRunMusicController.Instance?.UpdateTrack();
    }
    
    private void ApplyAwakeBounds()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null) return;

        var visuals = creatureNode.GetNode<Node>("Lagavulin");
    
        // Update root bounds on visuals
        var rootBounds = visuals.GetNodeOrNull<Control>("Bounds");
        var awakeBounds = visuals.GetNodeOrNull<Control>("AwakeBounds/Bounds");
        if (rootBounds != null && awakeBounds != null)
        {
            rootBounds.Position = awakeBounds.Position;
            rootBounds.Size = awakeBounds.Size;
        }

        // Update center position
        var rootCenter = visuals.GetNodeOrNull<Marker2D>("CenterPos");
        var awakeCenter = visuals.GetNodeOrNull<Marker2D>("AwakeBounds/CenterPos");
        if (rootCenter != null && awakeCenter != null)
        {
            rootCenter.Position = awakeCenter.Position;
        }

        // Update intent position
        var rootIntent = visuals.GetNodeOrNull<Marker2D>("IntentPos");
        var awakeIntent = visuals.GetNodeOrNull<Marker2D>("AwakeBounds/IntentPos");
        if (rootIntent != null && awakeIntent != null)
        {
            rootIntent.Position = awakeIntent.Position;
        }

        // Update hitbox on NCreature
        var hitbox = creatureNode.GetNodeOrNull<Control>("Hitbox");
        if (hitbox != null && awakeBounds != null)
        {
            hitbox.Position = awakeBounds.Position;
            hitbox.Size = awakeBounds.Size;
        }
    }

    public async Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        await Task.CompletedTask;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var sleepState = new MoveState(
            SLEEP,
            SleepMove,
            new AbstractIntent[] { new SleepIntent() }
        );

        var attackState = new MoveState(
            ATTACK,
            AttackMove,
            new AbstractIntent[] { new SingleAttackIntent(AttackDamage) }
        );

        var debuffState = new MoveState(
            DEBUFF,
            DebuffMove,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var mainBranch = new ConditionalBranchState("MAIN_BRANCH", SelectNextMove);

        sleepState.FollowUpState = mainBranch;
        attackState.FollowUpState = mainBranch;
        debuffState.FollowUpState = mainBranch;

        states.Add(sleepState);
        states.Add(attackState);
        states.Add(debuffState);
        states.Add(mainBranch);

        return new MonsterMoveStateMachine(states, mainBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (!IsAwake && Creature.HasPower<AsleepLagavulinPower>())
        {
            return SLEEP;
        }

        // First move when starting awake: Debuff
        if (StartsAwake && stateMachine.StateLog.Count == 0)
        {
            return DEBUFF;
        }

        if (DebuffTurnCount >= 2)
        {
            return DEBUFF;
        }

        if (LastTwoMoves(stateMachine, ATTACK))
        {
            return DEBUFF;
        }

        return ATTACK;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[^1].Id == moveId && log[^2].Id == moveId;
    }

    private Task SleepMove(IReadOnlyList<Creature> targets)
    {
        SleepTurnCount++;
        switch (SleepTurnCount)
        {
            case 1:
                TalkCmd.Play(_sleepLine1, Creature, VfxColor.White, VfxDuration.Long);
                break;
            case 2:
                TalkCmd.Play(_sleepLine2, Creature, VfxColor.White, VfxDuration.Long);
                break;
        }
        return Task.CompletedTask;
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        DebuffTurnCount++;

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task DebuffMove(IReadOnlyList<Creature> targets)
    {
        DebuffTurnCount = 0;

        await CreatureCmd.TriggerAnim(Creature, "Debuff", 0.0f);
        await Cmd.Wait(0.3f);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var sleepLoop = new AnimState("Idle_1", true)
        {
            BoundsContainer = "SleepingBounds"
        };
    
        var hurtSleeping = new AnimState("Idle_1", true);
        var wakeUp = new AnimState("Coming_out");
    
        var idleLoop = new AnimState("Idle_2", true)
        {
            BoundsContainer = "AwakeBounds"
        };
    
        var attack = new AnimState("Attack");
        var debuff = new AnimState("Debuff");
        var hurt = new AnimState("Hit");

        hurtSleeping.NextState = sleepLoop;
        wakeUp.NextState = idleLoop;
        attack.NextState = idleLoop;
        debuff.NextState = idleLoop;
        hurt.NextState = idleLoop;

        // Start in correct state based on StartsAwake
        var initialState = StartsAwake ? idleLoop : sleepLoop;
    
        var animator = new CreatureAnimator(initialState, controller);
        animator.AddAnyState("Sleep", sleepLoop);
        animator.AddAnyState("Wake", wakeUp, () => !IsAwake);
        animator.AddAnyState("Idle_2", idleLoop);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Debuff", debuff);
        animator.AddAnyState("Hit", hurt, () => IsAwake);
        animator.AddAnyState("Hit", hurtSleeping, () => !IsAwake);
    
        return animator;
    }
}