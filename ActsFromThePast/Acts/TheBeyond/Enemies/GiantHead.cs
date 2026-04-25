using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
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
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class GiantHead : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 520, 500);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 520, 500);

    private int StartingDeathDmg => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);
    private const int CountDamage = 13;
    private const int GlareDuration = 1;
    private const int IncrementDmg = 5;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/giant_head/giant_head.tscn";

    private const string GLARE = "GLARE";
    private const string IT_IS_TIME = "IT_IS_TIME";
    private const string COUNT = "COUNT";

    private static readonly LocString[] _timeDialogs = new[]
    {
        L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog1"),
        L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog2"),
        L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog3"),
        L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.IT_IS_TIME.dialog4"),
    };

    private int _count;

    private int Count
    {
        get => _count;
        set { AssertMutable(); _count = value; }
    }

    private int ItIsTimeDamage => StartingDeathDmg - Count * IncrementDmg;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _count = AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 4, 5);
        await PowerCmd.Apply<SlowPower>(new ThrowingPlayerChoiceContext(), Creature, 1M, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var glareState = new MoveState(
            GLARE,
            Glare,
            new AbstractIntent[] { new DebuffIntent() }
        );
        var itIsTimeState = new MoveState(
            IT_IS_TIME,
            ItIsTime,
            new DynamicSingleAttackIntent(() => ItIsTimeDamage)
        );
        var countState = new MoveState(
            COUNT,
            CountMove,
            new AbstractIntent[] { new SingleAttackIntent(CountDamage), new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        glareState.FollowUpState = moveBranch;
        itIsTimeState.FollowUpState = moveBranch;
        countState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { glareState, itIsTimeState, countState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (Count <= 1)
        {
            if (Count > -6)
                Count--;
            return IT_IS_TIME;
        }

        Count--;

        int num = rng.NextInt(100);
        if (num < 50)
        {
            if (!LastTwoMoves(stateMachine, GLARE))
                return GLARE;
            return COUNT;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, COUNT))
                return COUNT;
            return GLARE;
        }
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Glare(IReadOnlyList<Creature> targets)
    {
        PlaySfx();
        TalkCmd.Play(GetCountDialog(), Creature, VfxColor.DarkGray);
        await Cmd.Wait(0.5f);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, GlareDuration, Creature, null);
        }
    }

    private async Task ItIsTime(IReadOnlyList<Creature> targets)
    {
        PlaySfx();
        var dialog = _timeDialogs[Rng.Chaotic.NextInt(_timeDialogs.Length)];
        TalkCmd.Play(dialog, Creature, VfxColor.DarkGray);
        await Cmd.Wait(0.5f);
        await DamageCmd.Attack(ItIsTimeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task CountMove(IReadOnlyList<Creature> targets)
    {
        PlaySfx();
        TalkCmd.Play(GetCountDialog(), Creature, VfxColor.DarkGray);
        await Cmd.Wait(0.5f);
        await DamageCmd.Attack(CountDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private LocString GetCountDialog()
    {
        var dialog = L10NMonsterLookup("ACTSFROMTHEPAST-GIANT_HEAD.moves.COUNT.dialog");
        dialog.Add("count", Count.ToString());
        return dialog;
    }

    private void PlaySfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "giant_head_talk_1",
            1 => "giant_head_talk_2",
            _ => "giant_head_talk_3"
        };
        ModAudio.Play("giant_head", sfxName);
    }

    private void PlayDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "giant_head_death_1",
            1 => "giant_head_death_2",
            _ => "giant_head_death_3"
        };
        ModAudio.Play("giant_head", sfxName);
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        PlayDeathSfx();
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle_open", true);
        controller.GetAnimationState().SetTimeScale(0.5f);
        return new CreatureAnimator(idle, controller);
    }
}