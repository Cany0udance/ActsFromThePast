using BaseLib.Abstracts;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinNob : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 85, 82);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 90, 86);

    private int RushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);
    private int BashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);
    private const int VulnerableAmount = 2;
    private int EnrageAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_nob/gremlin_nob.tscn";

    private const string RUSH = "RUSH";
    private const string SKULL_BASH = "SKULL_BASH";
    private const string BELLOW = "BELLOW";
    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var bellowState = new MoveState(
            BELLOW,
            Bellow,
            new AbstractIntent[] { new BuffIntent() }
        );

        var rushState = new MoveState(
            RUSH,
            Rush,
            new AbstractIntent[] { new SingleAttackIntent(RushDamage) }
        );

        var skullBashState = new MoveState(
            SKULL_BASH,
            SkullBash,
            new AbstractIntent[] { new SingleAttackIntent(BashDamage), new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        bellowState.FollowUpState = moveBranch;
        rushState.FollowUpState = moveBranch;
        skullBashState.FollowUpState = moveBranch;

        states.Add(bellowState);
        states.Add(rushState);
        states.Add(skullBashState);
        states.Add(moveBranch);

        // Always starts with Bellow
        return new MonsterMoveStateMachine(states, bellowState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        // A18 behavior: Skull Bash if not used in last 2 turns, otherwise Rush (no repeat twice)
        if (!LastMove(stateMachine, SKULL_BASH) && !LastMoveBefore(stateMachine, SKULL_BASH))
        {
            return SKULL_BASH;
        }

        if (LastTwoMoves(stateMachine, RUSH))
        {
            return SKULL_BASH;
        }

        return RUSH;
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

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Bellow(IReadOnlyList<Creature> targets)
    {
        PlayBellowSfx();
        TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_NOB.moves.BELLOW.banter"), Creature, VfxColor.Red, VfxDuration.VeryLong);
    
        VfxCmd.PlayOnCreatureCenter(Creature, "vfx/vfx_scream");
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Long);
    
        await Cmd.Wait(0.8f);
    
        await PowerCmd.Apply<EnragePower>(Creature, EnrageAmount, Creature, null);
    }

    private async Task Rush(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(RushDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
    }

    private async Task SkullBash(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(BashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_gaze")
            .Execute(null);
    
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(target, VulnerableAmount, Creature, null);
        }
    }

    private void PlayBellowSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "bellow_1",
            1 => "bellow_2",
            _ => "bellow_3"
        };
        ModAudio.Play("gremlin_nob", sfxName);
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