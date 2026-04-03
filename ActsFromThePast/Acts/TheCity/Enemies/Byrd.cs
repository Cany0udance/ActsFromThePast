using ActsFromThePast.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Byrd : MonsterModel
{
    private static readonly LocString _cawLine = L10NMonsterLookup("BYRD.moves.CAW.speakLine");

    private bool _isFlying = true;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 33, 31);

    private int PeckDamage => 1;
    private int PeckCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private int SwoopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private const int HeadbuttDamage = 3;
    private const int CawStrength = 1;
    private int FlightAmount => 4;

    private const string FlyingVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_flying.tscn";
    private const string GroundedVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_grounded.tscn";

    protected override string VisualsPath => FlyingVisualsPath;

    public override IEnumerable<string> AssetPaths
    {
        get
        {
            var paths = new List<string>(base.AssetPaths) { GroundedVisualsPath };
            return paths;
        }
    }

    private const string PECK = "PECK";
    private const string SWOOP = "SWOOP";
    private const string CAW = "CAW";
    private const string GO_AIRBORNE = "GO_AIRBORNE";
    private const string HEADBUTT = "HEADBUTT";

    private bool IsFlying
    {
        get => _isFlying;
        set
        {
            AssertMutable();
            _isFlying = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<FlightPower>(Creature, FlightAmount, Creature, null);
    }

    public async Task OnFlightBroken()
    {
        IsFlying = false;
        await ByrdFallAnimation.Play(Creature, 100f);
        SwapVisuals(GroundedVisualsPath);

        // Squash the grounded visuals on impact
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var visuals = creatureNode.Visuals;
            var baseScale = visuals.Scale;
            var squashTween = creatureNode.CreateTween();
            squashTween.TweenProperty(visuals, "scale",
                new Vector2(baseScale.X * 1.2f, baseScale.Y * 0.8f), ByrdFallAnimation.SquashDuration * 0.5f);
            squashTween.TweenProperty(visuals, "scale",
                baseScale, ByrdFallAnimation.SquashDuration * 0.5f);
            await creatureNode.ToSignal(squashTween, Tween.SignalName.Finished);
        }

        await CreatureCmd.Stun(Creature, nextMoveId: HEADBUTT);
    }

    private void SwapVisuals(string newVisualsPath)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null)
            return;

        var oldVisuals = creatureNode.Visuals;
        var newVisuals = PreloadManager.Cache.GetScene(newVisualsPath).Instantiate<NCreatureVisuals>();

        // Use reflection to set the private setter on Visuals
        var visualsProp = typeof(NCreature).GetProperty("Visuals");
        visualsProp?.SetValue(creatureNode, newVisuals);

        creatureNode.AddChildSafely(newVisuals);
        creatureNode.MoveChild(newVisuals, 0);
        newVisuals.Position = Vector2.Zero;

        // Re-initialize the spine animator via reflection
        if (newVisuals.HasSpineAnimation)
        {
            var animator = GenerateAnimatorForState(newVisuals.SpineBody);
            var animatorField = typeof(NCreature).GetField("_spineAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(creatureNode, animator);
        }

        // Update bounds to match new visuals
        var updateBoundsMethod = typeof(NCreature).GetMethod("UpdateBounds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new[] { typeof(Node) }, null);
        updateBoundsMethod?.Invoke(creatureNode, new object[] { newVisuals });

        oldVisuals.QueueFree();
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        ModAudio.Play("byrd", "byrd_death");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var peckState = new MoveState(
            PECK,
            Peck,
            new AbstractIntent[] { new MultiAttackIntent(PeckDamage, PeckCount) }
        );

        var swoopState = new MoveState(
            SWOOP,
            Swoop,
            new AbstractIntent[] { new SingleAttackIntent(SwoopDamage) }
        );

        var cawState = new MoveState(
            CAW,
            Caw,
            new AbstractIntent[] { new BuffIntent() }
        );

        var goAirborneState = new MoveState(
            GO_AIRBORNE,
            GoAirborne,
            new AbstractIntent[] { new BuffIntent() }
        );

        var headbuttState = new MoveState(
            HEADBUTT,
            Headbutt,
            new AbstractIntent[] { new SingleAttackIntent(HeadbuttDamage) }
        );

        var flyingBranch = new ConditionalBranchState("FLYING_BRANCH", SelectFlyingMove);
        var firstMoveBranch = new ConditionalBranchState("FIRST_MOVE_BRANCH", SelectFirstMove);

        // Flying moves all loop back to flying branch
        peckState.FollowUpState = flyingBranch;
        swoopState.FollowUpState = flyingBranch;
        cawState.FollowUpState = flyingBranch;

        // Grounded sequence: headbutt -> go airborne -> back to flying
        headbuttState.FollowUpState = goAirborneState;
        goAirborneState.FollowUpState = flyingBranch;

        states.Add(peckState);
        states.Add(swoopState);
        states.Add(cawState);
        states.Add(goAirborneState);
        states.Add(headbuttState);
        states.Add(flyingBranch);
        states.Add(firstMoveBranch);

        return new MonsterMoveStateMachine(states, firstMoveBranch);
    }

    private string SelectFirstMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        return rng.NextFloat() < 0.375f ? CAW : PECK;
    }

    private string SelectFlyingMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 50)
        {
            if (LastTwoMoves(stateMachine, PECK))
                return rng.NextFloat() < 0.4f ? SWOOP : CAW;
            return PECK;
        }
        else if (num < 70)
        {
            if (LastMove(stateMachine, SWOOP))
                return rng.NextFloat() < 0.375f ? CAW : PECK;
            return SWOOP;
        }
        else
        {
            if (LastMove(stateMachine, CAW))
                return rng.NextFloat() < 0.2857f ? SWOOP : PECK;
            return CAW;
        }
    }

    private async Task Peck(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        for (int i = 0; i < PeckCount; i++)
        {
            PlayRandomBirdSfx();
            await DamageCmd.Attack(PeckDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
                .Execute(null);
        }
    }

    private async Task Swoop(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(SwoopDamage)
            .FromMonster(this)
            .WithHitFx(tmpSfx: "slash_attack.mp3")
            .WithHitVfxNode(target =>
            {
                var vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_scratch")).Instantiate<Node2D>();
                vfx.Scale = new Vector2(-1f, 1f);
                vfx.GlobalPosition = NCombatRoom.Instance?.GetCreatureNode(target)?.VfxSpawnPosition ?? Vector2.Zero;
                return vfx;
            })
            .Execute(null);
    }

    private async Task Caw(IReadOnlyList<Creature> targets)
    {
        ModAudio.Play("byrd", "byrd_death");
        TalkCmd.Play(_cawLine, Creature, VfxColor.Swamp, VfxDuration.Short);
        await PowerCmd.Apply<StrengthPower>(Creature, CawStrength, Creature, null);
    }

    private async Task GoAirborne(IReadOnlyList<Creature> targets)
    {
        IsFlying = true;
        SwapVisuals(FlyingVisualsPath);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var visuals = creatureNode.Visuals;
            visuals.Position = new Vector2(visuals.Position.X, visuals.Position.Y + 80f);

            // Rise up past the target
            await RiseAnimation.Play(Creature, 100f);

            // Settle back to correct position
            var settleTween = creatureNode.CreateTween();
            settleTween.TweenProperty(visuals, "position:y",
                    0f, 0.2f)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            await creatureNode.ToSignal(settleTween, Tween.SignalName.Finished);
        }
        ModAudio.Play("byrd", "flight");
        await PowerCmd.Apply<FlightPower>(Creature, FlightAmount, Creature, null);
    }

    private async Task Headbutt(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(HeadbuttDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private void PlayRandomBirdSfx()
    {
        var roll = Rng.Chaotic.NextInt(6) + 1;
        ModAudio.Play("byrd", $"byrd_talk_{roll}");
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
    
    // TODO investigate making head lift animation intermittently play during grounded non-stunned idle state

    private CreatureAnimator GenerateAnimatorForState(MegaSprite controller)
    {
        CreatureAnimator animator;

        if (IsFlying)
        {
            var idle = new AnimState("idle_flap", true);
            animator = new CreatureAnimator(idle, controller);
        }
        else
        {
            var idle = new AnimState("idle", true);
            var headLift = new AnimState("head_lift");
            headLift.NextState = idle;
            animator = new CreatureAnimator(idle, controller);
            animator.AddAnyState("Attack", headLift);
        }

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        animState.Apply(controller.GetSkeleton());

        return animator;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        return GenerateAnimatorForState(controller);
    }
}