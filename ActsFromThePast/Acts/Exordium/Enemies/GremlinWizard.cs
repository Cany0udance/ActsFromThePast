using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinWizard : CustomMonsterModel
{
    private static readonly LocString _chargingDialog = L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.CHARGING.dialog");
    private static readonly LocString _ultimateDialog = L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.ULTIMATE_BLAST.dialog");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);

    private int UltimateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);

    private const int ChargeLimit = 3;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_wizard/gremlin_wizard.tscn";

    private const string CHARGING = "CHARGING";
    private const string ULTIMATE_BLAST = "ULTIMATE_BLAST";

    private int _currentCharge = 1;
    private int CurrentCharge
    {
        get => _currentCharge;
        set
        {
            AssertMutable();
            _currentCharge = value;
        }
    }
    

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var chargingState = new MoveState(
            CHARGING,
            Charging,
            new AbstractIntent[] { new UnknownIntent() }
        );

        var ultimateBlastState = new MoveState(
            ULTIMATE_BLAST,
            UltimateBlast,
            new AbstractIntent[] { new SingleAttackIntent(UltimateDamage) }
        );

        // After charging, check if ready to blast
        chargingState.FollowUpState = new ConditionalBranchState("AFTER_CHARGE", SelectAfterCharge);
        
        // After blast, just blast again (A17 behavior as default)
        ultimateBlastState.FollowUpState = ultimateBlastState;

        states.Add(chargingState);
        states.Add(ultimateBlastState);
        states.Add(chargingState.FollowUpState);

        return new MonsterMoveStateMachine(states, chargingState);
    }

    private string SelectAfterCharge(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        return CurrentCharge >= ChargeLimit ? ULTIMATE_BLAST : CHARGING;
    }

    private Task Charging(IReadOnlyList<Creature> targets)
    {
        CurrentCharge++;
        if (CurrentCharge >= ChargeLimit)
        {
            PlayRandomChargeSfx();
            TalkCmd.Play(_ultimateDialog, Creature, VfxColor.Purple, VfxDuration.Long);
        }
        return Task.CompletedTask;
    }

    private async Task UltimateBlast(IReadOnlyList<Creature> targets)
    {
        CurrentCharge = 0;

        await DamageCmd.Attack(UltimateDamage)
            .FromMonster(this) 
            .WithHitFx("vfx/vfx_fire_burst", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private void PlayRandomChargeSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "gremlin_wizard_talk_1",
            _ => "gremlin_wizard_talk_2"
        };
        ModAudio.Play("gremlin_wizard", sfxName);
    }

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
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "gremlin_wizard_death_1",
            1 => "gremlin_wizard_death_2",
            _ => "gremlin_wizard_death_3"
        };
        ModAudio.Play("gremlin_wizard", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("animation", true);
        var animator = new CreatureAnimator(idle, controller);

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        animState.Apply(controller.GetSkeleton());

        return animator;
    }
}