using BaseLib.Abstracts;
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
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Cultist : CustomMonsterModel
{
    private static readonly LocString _incantationLine1 = L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.moves.INCANTATION.speakLine1");
    private static readonly LocString _incantationLine2 = L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.moves.INCANTATION.speakLine2");
    private static readonly LocString _deathLine = L10NMonsterLookup("ACTSFROMTHEPAST-CULTIST.deathLine");
    
    private bool _saidPower;
    
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 56, 54);
    private int AttackDamage => 6;
    private int RitualAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    
    protected override string VisualsPath => "res://ActsFromThePast/monsters/cultist/cultist.tscn";
    
    private bool SaidPower
    {
        get => _saidPower;
        set
        {
            AssertMutable();
            _saidPower = value;
        }
    }
    
    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
    
        // Only trigger for this specific Cultist
        if (creature != Creature)
            return;

        PlayDeathSfx();

        if (SaidPower)
        {
            var text = _deathLine.GetFormattedText();
            var bubble = NSpeechBubbleVfx.Create(text, Creature, 2.5);
            if (bubble != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
                await Cmd.Wait(2.5f);
            }
        }
    }
    
    private void PlayDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "cultist_death_1",
            1 => "cultist_death_2",
            _ => "cultist_death_3"
        };
        ModAudio.Play("cultist", sfxName);
    }

    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var incantation = new MoveState(
            "INCANTATION",
            Incantation,
            new BuffIntent()
        );
        var darkStrike = new MoveState(
            "DARK_STRIKE",
            DarkStrike,
            new SingleAttackIntent(AttackDamage)
        );
        
        incantation.FollowUpState = darkStrike;
        darkStrike.FollowUpState = darkStrike;
        
        return new MonsterMoveStateMachine(
            new List<MonsterState> { incantation, darkStrike },
            incantation
        );
    }
    
    private async Task Incantation(IReadOnlyList<Creature> targets)
    {
        PlayIncantationSfx();

        var player = Creature.CombatState?.Players.FirstOrDefault();
        var currentActIndex = player?.RunState.CurrentActIndex ?? -1;

        if (currentActIndex == 0)
        {
            var roll = Rng.Chaotic.NextInt(10);
            if (roll < 3)
            {
                TalkCmd.Play(_incantationLine1, Creature, VfxColor.Blue, VfxDuration.Long);
                SaidPower = true;
            }
            else if (roll < 6)
            {
                TalkCmd.Play(_incantationLine2, Creature, VfxColor.Blue, VfxDuration.Long);
            }
        }

        await Cmd.Wait(0.5f);
        await PowerCmd.Apply<RitualPower>(Creature, RitualAmount, Creature, null);
    }
    
    private void PlayIncantationSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "cultist_talk_1",
            1 => "cultist_talk_2",
            _ => "cultist_talk_3"
        };
        ModAudio.Play("cultist", sfxName);
    }
    
    private async Task DarkStrike(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }
    
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("waving", true);
        return new CreatureAnimator(idle, controller);
    }
}