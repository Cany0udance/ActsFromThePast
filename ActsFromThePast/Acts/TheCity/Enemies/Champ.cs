using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Champ : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 440, 420);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 440, 420);

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
    private int ExecuteDamage => 10;
    private int SlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int ForgeAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 7, 5);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 20, 15);

    private const int DebuffAmount = 2;
    private const int ExecuteCount = 2;
    private const int ForgeThreshold = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/champ/champ.tscn";

    private static readonly LocString _tauntLine1 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog1");
    private static readonly LocString _tauntLine2 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog2");
    private static readonly LocString _tauntLine3 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog3");
    private static readonly LocString _tauntLine4 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.TAUNT.dialog4");
    private static readonly LocString _limitBreakLine1 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.ANGER.dialog1");
    private static readonly LocString _limitBreakLine2 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.ANGER.dialog2");
    private static readonly LocString _deathLine1 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.EXECUTE.dialog1");
    private static readonly LocString _deathLine2 = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.moves.EXECUTE.dialog2");
    private static readonly LocString _beltLine = L10NMonsterLookup("ACTSFROMTHEPAST-CHAMP.beltDialog");

    private const string HEAVY_SLASH = "HEAVY_SLASH";
    private const string DEFENSIVE_STANCE = "DEFENSIVE_STANCE";
    private const string EXECUTE = "EXECUTE";
    private const string FACE_SLAP = "FACE_SLAP";
    private const string GLOAT = "GLOAT";
    private const string TAUNT = "TAUNT";
    private const string ANGER = "ANGER";

    private int _numTurns;
    private int _forgeTimes;
    private bool _thresholdReached;
    private bool _firstTurn;

    private int NumTurns
    {
        get => _numTurns;
        set
        {
            AssertMutable();
            _numTurns = value;
        }
    }

    private int ForgeTimes
    {
        get => _forgeTimes;
        set
        {
            AssertMutable();
            _forgeTimes = value;
        }
    }

    private bool ThresholdReached
    {
        get => _thresholdReached;
        set
        {
            AssertMutable();
            _thresholdReached = value;
        }
    }

    private bool FirstTurn
    {
        get => _firstTurn;
        set
        {
            AssertMutable();
            _firstTurn = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _numTurns = 0;
        _forgeTimes = 0;
        _thresholdReached = false;
        _firstTurn = true;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);

        if (creature != Creature)
            return;

        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Long);
        PlayDeathSfx();
    }

    private void PlayDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "champ_death_1",
            _ => "champ_death_2"
        };
        ModAudio.Play("champ", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var heavySlashState = new MoveState(
            HEAVY_SLASH,
            HeavySlash,
            new AbstractIntent[] { new SingleAttackIntent(SlashDamage) }
        );

        var defensiveStanceState = new MoveState(
            DEFENSIVE_STANCE,
            DefensiveStance,
            new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
        );

        var executeState = new MoveState(
            EXECUTE,
            Execute,
            new AbstractIntent[] { new MultiAttackIntent(ExecuteDamage, ExecuteCount) }
        );

        var faceSlapState = new MoveState(
            FACE_SLAP,
            FaceSlap,
            new AbstractIntent[] { new SingleAttackIntent(SlapDamage), new DebuffIntent() }
        );

        var gloatState = new MoveState(
            GLOAT,
            Gloat,
            new AbstractIntent[] { new BuffIntent() }
        );

        var tauntState = new MoveState(
            TAUNT,
            Taunt,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var angerState = new MoveState(
            ANGER,
            Anger,
            new AbstractIntent[] { new BuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        heavySlashState.FollowUpState = moveBranch;
        defensiveStanceState.FollowUpState = moveBranch;
        executeState.FollowUpState = moveBranch;
        faceSlapState.FollowUpState = moveBranch;
        gloatState.FollowUpState = moveBranch;
        tauntState.FollowUpState = moveBranch;
        angerState.FollowUpState = moveBranch;

        states.Add(heavySlashState);
        states.Add(defensiveStanceState);
        states.Add(executeState);
        states.Add(faceSlapState);
        states.Add(gloatState);
        states.Add(tauntState);
        states.Add(angerState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        NumTurns++;

        if (Creature.CurrentHp < Creature.MaxHp / 2 && !ThresholdReached)
        {
            ThresholdReached = true;
            return ANGER;
        }

        if (ThresholdReached && !LastMove(stateMachine, EXECUTE) && !LastMoveBefore(stateMachine, EXECUTE))
        {
            var deathQuote = Rng.Chaotic.NextInt(2) == 0 ? _deathLine1 : _deathLine2;
            TalkCmd.Play(deathQuote, Creature, VfxColor.Blue, VfxDuration.Long);
            return EXECUTE;
        }

        // Every 4th turn before threshold: Taunt
        if (NumTurns == 4 && !ThresholdReached)
        {
            NumTurns = 0;
            return TAUNT;
        }

        int num = rng.NextInt(100);

        // Defensive Stance (A19+: 30% chance, otherwise handled similarly — we port highest ascension)
        if (!LastMove(stateMachine, DEFENSIVE_STANCE) && ForgeTimes < ForgeThreshold && num < 30)
        {
            ForgeTimes++;
            return DEFENSIVE_STANCE;
        }

        // Gloat
        if (!LastMove(stateMachine, GLOAT) && !LastMove(stateMachine, DEFENSIVE_STANCE) && num < 30)
        {
            return GLOAT;
        }

        // Face Slap
        if (!LastMove(stateMachine, FACE_SLAP) && num < 55)
        {
            return FACE_SLAP;
        }

        // Heavy Slash (fallback)
        if (!LastMove(stateMachine, HEAVY_SLASH))
        {
            return HEAVY_SLASH;
        }

        // If Heavy Slash was last move, fall back to Face Slap
        return FACE_SLAP;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private static bool LastMoveBefore(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 2].Id == moveId;
    }

    private async Task HeavySlash(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "HeavySlash", 0.0f);
        await Cmd.Wait(0.4f);

        await DamageCmd.Attack(SlashDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/ironclad/ironclad_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DefensiveStance(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
        await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), Creature, ForgeAmount, Creature, null);
    }

    private async Task Execute(IReadOnlyList<Creature> targets)
    {
        await JumpAnimation.Play(Creature);
        await Cmd.Wait(0.5f);

        for (int i = 0; i < ExecuteCount; i++)
        {
            await DamageCmd.Attack(ExecuteDamage)
                .FromMonster(this)
                .WithAttackerFx(sfx: "event:/sfx/characters/ironclad/ironclad_attack")
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }

    private async Task FaceSlap(IReadOnlyList<Creature> targets)
    {
        ModAudio.Play("champ", "champ_slap");
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(SlapDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
        }
    }

    private async Task Gloat(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);
    }

    private async Task Taunt(IReadOnlyList<Creature> targets)
    {
        var tauntLines = new[] { _tauntLine1, _tauntLine2, _tauntLine3, _tauntLine4 };
        var taunt = tauntLines[Rng.Chaotic.NextInt(tauntLines.Length)];
        ModAudio.Play("champ", "champ_taunt");
        TalkCmd.Play(taunt, Creature, VfxColor.Blue, VfxDuration.Long);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
        }
    }

    private async Task Anger(IReadOnlyList<Creature> targets)
    {
        var limitBreakLines = new[] { _limitBreakLine1, _limitBreakLine2 };
        var line = limitBreakLines[Rng.Chaotic.NextInt(limitBreakLines.Length)];
        ModAudio.Play("champ", "champ_charge");
        TalkCmd.Play(line, Creature, VfxColor.Blue, VfxDuration.VeryLong);
        // TODO: Inflame VFX
        await Cmd.Wait(0.75f);
        
        var debuffs = Creature.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .ToList();
        foreach (var debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, StrengthAmount * 3, Creature, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("HeavySlash", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}