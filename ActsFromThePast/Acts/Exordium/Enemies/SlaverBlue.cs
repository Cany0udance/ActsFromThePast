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
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SlaverBlue : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 50);

    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);
    private int RakeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/slaver_blue/slaver_blue.tscn";
    public override bool HasDeathSfx => false;

    private const string STAB = "STAB";
    private const string RAKE = "RAKE";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slaver_blue_death_1",
            _ => "slaver_blue_death_2"
        };
        ModAudio.Play("slaver_blue", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var stabState = new MoveState(
            STAB,
            Stab,
            new AbstractIntent[] { new SingleAttackIntent(StabDamage) }
        );

        var rakeState = new MoveState(
            RAKE,
            Rake,
            new AbstractIntent[] { new SingleAttackIntent(RakeDamage), new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        stabState.FollowUpState = moveBranch;
        rakeState.FollowUpState = moveBranch;

        states.Add(stabState);
        states.Add(rakeState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        // 60% chance to Stab if haven't used it twice in a row
        if (num >= 40 && !LastTwoMoves(stateMachine, STAB))
        {
            return STAB;
        }

        // Otherwise Rake if not used last turn (A17 behavior)
        if (!LastMove(stateMachine, RAKE))
        {
            return RAKE;
        }

        return STAB;
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

    private async Task Stab(IReadOnlyList<Creature> targets)
    {
        PlayAttackSfx();
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(StabDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "slash_attack.mp3")
            .Execute(null);
    }

    private async Task Rake(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(RakeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "slash_attack.mp3")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, WeakAmount, Creature, null);
        }
    }

    private void PlayAttackSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slaver_blue_talk_1",
            _ => "slaver_blue_talk_2"
        };
        ModAudio.Play("slaver_blue", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}