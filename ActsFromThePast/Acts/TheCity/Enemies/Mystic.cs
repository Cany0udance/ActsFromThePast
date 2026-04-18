using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Mystic : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 58, 56);

    private int MagicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int HealAmount => 20 * CombatState.Players.Count;
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private const int FrailAmount = 2;
    private int HealThreshold => 20 * CombatState.Players.Count;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/mystic/mystic.tscn";

    private const string ATTACK = "ATTACK";
    private const string HEAL = "HEAL";
    private const string BUFF = "BUFF";

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
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "mystic_death_1",
            1 => "mystic_death_2",
            _ => "mystic_death_3"
        };
        ModAudio.Play("mystic", sfxName);
    }

    private void PlayTurnSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "mystic_talk_1",
            _ => "mystic_talk_2"
        };
        ModAudio.Play("mystic", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var attackState = new MoveState(
            ATTACK,
            Attack,
            new AbstractIntent[] { new SingleAttackIntent(MagicDamage), new DebuffIntent() }
        );
        var healState = new MoveState(
            HEAL,
            Heal,
            new AbstractIntent[] { new HealIntent() }
        );
        var buffState = new MoveState(
            BUFF,
            Buff,
            new AbstractIntent[] { new BuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        attackState.FollowUpState = moveBranch;
        healState.FollowUpState = moveBranch;
        buffState.FollowUpState = moveBranch;

        states.Add(attackState);
        states.Add(healState);
        states.Add(buffState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        // Priority 1: Heal if teammates are missing enough HP
        var teammates = CombatState.GetTeammatesOf(Creature);
        int totalMissingHp = 0;
        foreach (var teammate in teammates)
        {
            if (teammate.IsAlive)
            {
                totalMissingHp += teammate.MaxHp - teammate.CurrentHp;
            }
        }

        if (totalMissingHp > HealThreshold && !LastTwoMoves(stateMachine, HEAL))
        {
            return HEAL;
        }

        // Priority 2: Attack (40%+ roll, can't repeat)
        int num = rng.NextInt(100);
        if (num >= 40 && !LastMove(stateMachine, ATTACK))
        {
            return ATTACK;
        }

        // Priority 3: Buff, unless last two were buff
        if (!LastTwoMoves(stateMachine, BUFF))
        {
            return BUFF;
        }

        // Fallback: Attack
        return ATTACK;
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

    private async Task Attack(IReadOnlyList<Creature> targets)
    {
        PlayTurnSfx();
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack((decimal)MagicDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        await PowerCmd.Apply<FrailPower>(targets, (decimal)FrailAmount, Creature, (CardModel)null);
    }

    private async Task Heal(IReadOnlyList<Creature> targets)
    {
        PlayTurnSfx();
        await CreatureCmd.TriggerAnim(Creature, "Heal", 0.0f);
        await Cmd.Wait(0.25f);

        var teammates = CombatState.GetTeammatesOf(Creature);
        foreach (var teammate in teammates)
        {
            if (teammate.IsAlive)
            {
                await CreatureCmd.Heal(teammate, (decimal)HealAmount);
            }
        }
    }

    private async Task Buff(IReadOnlyList<Creature> targets)
    {
        PlayTurnSfx();
        await CreatureCmd.TriggerAnim(Creature, "Heal", 0.0f);
        await Cmd.Wait(0.25f);

        var teammates = CombatState.GetTeammatesOf(Creature);
        foreach (var teammate in teammates)
        {
            if (teammate.IsAlive)
            {
                await PowerCmd.Apply<StrengthPower>(teammate, (decimal)StrengthAmount, Creature, (CardModel)null);
            }
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var heal = new AnimState("Attack");
        var hit = new AnimState("Hit");

        heal.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Heal", heal);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}