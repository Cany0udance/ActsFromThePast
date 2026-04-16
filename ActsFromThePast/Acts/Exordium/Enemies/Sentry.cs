using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Sentry : CustomMonsterModel
{
    public const string BOLT = "BOLT";
    public const string BEAM = "BEAM";

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 39, 38);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 42);

    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);
    private int DazedAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/sentry/sentry.tscn";

    private bool _boltFirst;

    public bool BoltFirst
    {
        get => _boltFirst;
        set
        {
            AssertMutable();
            _boltFirst = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(Creature, 1m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var boltState = new MoveState(
            BOLT,
            BoltMove,
            new AbstractIntent[] { new StatusIntent(DazedAmount) }
        );

        var beamState = new MoveState(
            BEAM,
            BeamMove,
            new AbstractIntent[] { new SingleAttackIntent(BeamDamage) }
        );

        boltState.FollowUpState = beamState;
        beamState.FollowUpState = boltState;

        states.Add(boltState);
        states.Add(beamState);

        var initialState = BoltFirst ? boltState : beamState;

        return new MonsterMoveStateMachine(states, initialState);
    }

    private async Task BoltMove(IReadOnlyList<Creature> targets)
    {
        var spazAnim = Rng.Chaotic.NextInt(3) switch
        {
            0 => "spaz1",
            1 => "spaz2",
            _ => "spaz3"
        };
        await CreatureCmd.TriggerAnim(Creature, spazAnim, 0.0f);

        ModAudio.Play("general", "thunderclap");

        var sentryPos = Sts1VfxHelper.GetCreatureCenter(Creature);
        ShockWaveEffect.PlayRoyal(sentryPos);

        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        await Cmd.Wait(0.5f);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, DazedAmount, false);
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.0f);
    
        BorderFlashEffect.PlaySky();
        ModAudio.Play("general", "magic_beam_short", -6f);
    
        var playerCreature = targets.FirstOrDefault(c => c.Player != null);
        var playerPos = playerCreature != null 
            ? Sts1VfxHelper.GetCreatureCenter(playerCreature) 
            : Vector2.Zero;
        var sentryPos = Sts1VfxHelper.GetCreatureCenter(Creature);
    
        var laser = SmallLaserEffect.Create(sentryPos, playerPos);
        Sts1VfxHelper.Play(laser);
    
        await Cmd.Wait(0.3f);
        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .Execute(null);
    }

public override CreatureAnimator GenerateAnimator(MegaSprite controller)
{
    var idle = new AnimState("idle", true);
    var attack = new AnimState("attack");
    var spaz1 = new AnimState("spaz1");
    var spaz2 = new AnimState("spaz2");
    var spaz3 = new AnimState("spaz3");
    var hit = new AnimState("hit");
    attack.NextState = idle;
    spaz1.NextState = idle;
    spaz2.NextState = idle;
    spaz3.NextState = idle;
    hit.NextState = idle;
    var animator = new CreatureAnimator(idle, controller);
    animator.AddAnyState("Attack", attack);
    animator.AddAnyState("spaz1", spaz1);
    animator.AddAnyState("spaz2", spaz2);
    animator.AddAnyState("spaz3", spaz3);
    animator.AddAnyState("Hit", hit);

    var animState = controller.GetAnimationState();
    animState.SetTimeScale(2.0f);
    var current = animState.GetCurrent(0);
    current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
    animState.Update(0.0f);
    animState.Apply(controller.GetSkeleton());

    return animator;
}
}