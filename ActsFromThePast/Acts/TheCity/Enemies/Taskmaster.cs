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
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Taskmaster : MonsterModel
{
    // TODO UNPREMULTIPLY ALPHA
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 57, 54);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 64, 60);
    
    private int ScouringWhipDamage => 7;
    private int WoundCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 1);
    private bool GainsStrength => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);
    
    protected override string VisualsPath => "res://ActsFromThePast/monsters/taskmaster/taskmaster.tscn";
    
    private const string SCOURING_WHIP = "SCOURING_WHIP";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }
    
    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayDeathSfx();
    }
    
    private void PlayDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "taskmaster_death_1",
            _ => "taskmaster_death_2"
        };
        ModAudio.Play("taskmaster", sfxName);
    }
    
    private void PlayAttackSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "taskmaster_talk_1",
            _ => "taskmaster_talk_2"
        };
        ModAudio.Play("taskmaster", sfxName);
    }
    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var scouringWhipState = new MoveState(
            SCOURING_WHIP,
            ScouringWhip,
            new AbstractIntent[] { new SingleAttackIntent(ScouringWhipDamage), new StatusIntent(WoundCount) }
        );
        
        scouringWhipState.FollowUpState = scouringWhipState;
        
        return new MonsterMoveStateMachine(
            new List<MonsterState> { scouringWhipState },
            scouringWhipState
        );
    }
    
    private async Task ScouringWhip(IReadOnlyList<Creature> targets)
    {
        PlayAttackSfx();
        
        await FastAttackAnimation.Play(Creature);
        
        await DamageCmd.Attack(ScouringWhipDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "slash_attack.mp3")
            .Execute(null);
        
        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Discard, WoundCount, false);
        
        if (GainsStrength)
        {
            await PowerCmd.Apply<StrengthPower>(Creature, 1, Creature, null);
        }
    }
    
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}