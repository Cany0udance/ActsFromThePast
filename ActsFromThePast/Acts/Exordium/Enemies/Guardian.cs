using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Guardian : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);

    private int FierceBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 36, 32);
    private int RollDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);
    private const int WhirlwindDamage = 5;
    private const int WhirlwindCount = 4;
    private const int TwinSlamDamage = 8;
    private const int TwinSlamHits = 2;
    private const int DefensiveBlock = 20;
    private const int ChargeUpBlock = 9;
    private int SharpHideThorns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private const int VentDebuffAmount = 2;
    private int DmgThresholdBase => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 30);
    private const int DmgThresholdIncrease = 10;

    private int _currentDmgThreshold;
    public int CurrentDmgThreshold => _currentDmgThreshold;

    private int _dmgTaken;
    public int DmgTaken
    {
        get => _dmgTaken;
        set
        {
            AssertMutable();
            _dmgTaken = value;
        }
    }

    private bool _isOpen = true;
    public bool IsOpen => _isOpen;

    private bool _closeUpTriggered;
    public bool CloseUpTriggered
    {
        get => _closeUpTriggered;
        set
        {
            AssertMutable();
            _closeUpTriggered = value;
        }
    }

    private MoveState _closeUpState;
    private static readonly LocString _destroyDialog = L10NMonsterLookup("GUARDIAN.moves.CHARGE_UP.dialog");

    protected override string VisualsPath => "res://ActsFromThePast/monsters/guardian/guardian.tscn";

    private const string CLOSE_UP = "CLOSE_UP";
    private const string FIERCE_BASH = "FIERCE_BASH";
    private const string ROLL_ATTACK = "ROLL_ATTACK";
    private const string TWIN_SLAM = "TWIN_SLAM";
    private const string WHIRLWIND = "WHIRLWIND";
    private const string CHARGE_UP = "CHARGE_UP";
    private const string VENT_STEAM = "VENT_STEAM";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _currentDmgThreshold = DmgThresholdBase;
        _dmgTaken = 0;
        await PowerCmd.Apply<ModeShiftPower>(Creature, _currentDmgThreshold, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        // Offensive mode states
        var chargeUpState = new MoveState(
            CHARGE_UP,
            ChargeUp,
            new AbstractIntent[] { new DefendIntent() }
        );

        var fierceBashState = new MoveState(
            FIERCE_BASH,
            FierceBash,
            new AbstractIntent[] { new SingleAttackIntent(FierceBashDamage) }
        );

        var ventSteamState = new MoveState(
            VENT_STEAM,
            VentSteam,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var whirlwindState = new MoveState(
            WHIRLWIND,
            Whirlwind,
            new AbstractIntent[] { new MultiAttackIntent(WhirlwindDamage, WhirlwindCount) }
        );

        // Defensive mode states
        _closeUpState = new MoveState(
            CLOSE_UP,
            CloseUp,
            new AbstractIntent[] { new BuffIntent() }
        );

        var rollAttackState = new MoveState(
            ROLL_ATTACK,
            RollAttack,
            new AbstractIntent[] { new SingleAttackIntent(RollDamage) }
        );

        var twinSlamState = new MoveState(
            TWIN_SLAM,
            TwinSlam,
            new AbstractIntent[] { new MultiAttackIntent(TwinSlamDamage, TwinSlamHits), new BuffIntent() }
        );

        // Offensive mode cycle: ChargeUp -> FierceBash -> VentSteam -> Whirlwind -> (repeat)
        chargeUpState.FollowUpState = fierceBashState;
        fierceBashState.FollowUpState = ventSteamState;
        ventSteamState.FollowUpState = whirlwindState;
        whirlwindState.FollowUpState = chargeUpState;

        // Defensive mode cycle: CloseUp -> RollAttack -> TwinSlam -> (back to Whirlwind in offensive)
        _closeUpState.FollowUpState = rollAttackState;
        rollAttackState.FollowUpState = twinSlamState;
        twinSlamState.FollowUpState = whirlwindState;

        states.Add(chargeUpState);
        states.Add(fierceBashState);
        states.Add(ventSteamState);
        states.Add(whirlwindState);
        states.Add(_closeUpState);
        states.Add(rollAttackState);
        states.Add(twinSlamState);

        return new MonsterMoveStateMachine(states, chargeUpState);
    }

    private async Task ChargeUp(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, ChargeUpBlock, ValueProp.Move, null);
        ModAudio.Play("guardian", "guardian_destroy");
        TalkCmd.Play(_destroyDialog, Creature, 2.5);
    }

    private async Task FierceBash(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(FierceBashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task VentSteam(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, VentDebuffAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(target, VentDebuffAmount, Creature, null);
        }
    }

    private async Task Whirlwind(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        ModAudio.Play("general", "whirlwind");
    
        for (int i = 0; i < WhirlwindCount; i++)
        {
            ModAudio.Play("general", "attack_heavy");
        
            var target = targets.FirstOrDefault(t => t.IsAlive);
            if (target != null)
            {
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var cleaveVfx = CleaveEffect.Create(targetNode.VfxSpawnPosition);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(cleaveVfx);
                }
            }
        
            await Cmd.Wait(0.15f);
        
            await DamageCmd.Attack(WhirlwindDamage)
                .FromMonster(this)
                .Execute(null);
        }
    }

    private async Task CloseUp(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<SharpHidePower>(Creature, SharpHideThorns, Creature, null);
    }

    private async Task RollAttack(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(RollDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task TwinSlam(IReadOnlyList<Creature> targets)
    {
        await TransitionToOffensiveMode();

        await DamageCmd.Attack(TwinSlamDamage)
            .WithHitCount(TwinSlamHits)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Remove<SharpHidePower>(Creature);
    }

    public async Task TransitionToDefensiveMode()
    {
        await PowerCmd.Remove<ModeShiftPower>(Creature);
        ModAudio.Play("guardian", "guardian_boss_transform");
        await CreatureCmd.GainBlock(Creature, DefensiveBlock, ValueProp.Move, null);
    
        // Trigger animator to update bounds
        await CreatureCmd.TriggerAnim(Creature, "transition", 0.0f);
    
        // Now take over with speed modification
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
    
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var trackEntry = animState.GetCurrent(0);
            if (trackEntry != null)
            {
                trackEntry.SetTimeScale(2.0f);
                var duration = trackEntry.GetAnimationEnd() / 2.0f;
                await Cmd.Wait(duration);
            }
        
            animState.SetAnimation("defensive", true, 0);
        }
    
        _currentDmgThreshold += DmgThresholdIncrease;
        _isOpen = false;
        SetMoveImmediate(_closeUpState, true);
    }

    private async Task TransitionToOffensiveMode()
    {
        _dmgTaken = 0;
        await PowerCmd.Apply<ModeShiftPower>(Creature, _currentDmgThreshold, Creature, null);
        if (Creature.Block > 0)
        {
            await CreatureCmd.LoseBlock(Creature, Creature.Block);
        }
    
        // Trigger animator to update bounds
        await CreatureCmd.TriggerAnim(Creature, "idle", 0.0f);
    
        // Apply speed modification if needed
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
    
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var trackEntry = animState.GetCurrent(0);
            trackEntry.SetMixDuration(0.2f);
        }
    
        _isOpen = true;
        _closeUpTriggered = false;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true)
        {
            BoundsContainer = "IdleBounds"
        };
    
        var defensive = new AnimState("defensive", true)
        {
            BoundsContainer = "DefensiveBounds"
        };
    
        var transition = new AnimState("transition");
    
        transition.NextState = defensive;
        idle.AddBranch("transition", transition);
        idle.AddBranch("defensive", defensive);
        defensive.AddBranch("idle", idle);
    
        return new CreatureAnimator(idle, controller);
    }
}