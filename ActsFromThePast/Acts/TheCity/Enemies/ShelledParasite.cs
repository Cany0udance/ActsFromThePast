using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class ShelledParasite : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 70, 68);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 75, 72);

    private int FellDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);
    private int DoubleStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int SuckDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private const int PlatedArmorAmount = 14;
    private const int DoubleStrikeCount = 2;
    private const int FellFrailAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/shelled_parasite/shelled_parasite.tscn";

    private const string FELL = "FELL";
    private const string DOUBLE_STRIKE = "DOUBLE_STRIKE";
    private const string LIFE_SUCK = "LIFE_SUCK";
    private const string STUNNED = "STUNNED";
    private MoveState _stunnedState;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<PlatedArmorPower>(new ThrowingPlayerChoiceContext(), Creature, PlatedArmorAmount, Creature, null);
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var fellState = new MoveState(
            FELL,
            Fell,
            new AbstractIntent[] { new SingleAttackIntent(FellDamage), new DebuffIntent() }
        );

        var doubleStrikeState = new MoveState(
            DOUBLE_STRIKE,
            DoubleStrike,
            new AbstractIntent[] { new MultiAttackIntent(DoubleStrikeDamage, DoubleStrikeCount) }
        );

        var lifeSuckState = new MoveState(
            LIFE_SUCK,
            LifeSuck,
            new AbstractIntent[] { new SingleAttackIntent(SuckDamage), new HealIntent() }
        );

        _stunnedState = new MoveState(
            STUNNED,
            Stunned,
            new AbstractIntent[] { new StunIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        fellState.FollowUpState = moveBranch;
        doubleStrikeState.FollowUpState = moveBranch;
        lifeSuckState.FollowUpState = moveBranch;
        _stunnedState.FollowUpState = fellState;

        states.Add(fellState);
        states.Add(doubleStrikeState);
        states.Add(lifeSuckState);
        states.Add(_stunnedState);
        states.Add(moveBranch);

        // A17+: always opens with Fell
        return new MonsterMoveStateMachine(states, fellState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 20)
        {
            if (!LastMove(stateMachine, FELL))
                return FELL;
            return SelectNextMove(owner, rng, stateMachine, 20);
        }
        else if (num < 60)
        {
            if (!LastTwoMoves(stateMachine, DOUBLE_STRIKE))
                return DOUBLE_STRIKE;
            return LIFE_SUCK;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, LIFE_SUCK))
                return LIFE_SUCK;
            return DOUBLE_STRIKE;
        }
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine, int min)
    {
        int num = rng.NextInt(min, 100);

        if (num < 60)
        {
            if (!LastTwoMoves(stateMachine, DOUBLE_STRIKE))
                return DOUBLE_STRIKE;
            return LIFE_SUCK;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, LIFE_SUCK))
                return LIFE_SUCK;
            return DOUBLE_STRIKE;
        }
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

    private async Task Fell(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(FellDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, FellFrailAmount, Creature, null);
        }
    }

    private async Task DoubleStrike(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < DoubleStrikeCount; i++)
        {
            await HopAnimation.Play(Creature);
            await Cmd.Wait(0.2f);

            await DamageCmd.Attack(DoubleStrikeDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
                .Execute(null);
        }
    }

    private async Task LifeSuck(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Bite", 0.0f);
        await Cmd.Wait(0.4f);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (targetNode != null)
            {
                var position = targetNode.VfxSpawnPosition;
                var effect = BiteEffect.Create(position);
                NCombatRoom.Instance.CombatVfxContainer.AddChild(effect);
                effect.GlobalPosition = position;
            }
        }

        await Cmd.Wait(0.3f);

        var attack = await DamageCmd.Attack(SuckDamage)
            .FromMonster(this)
            .Execute(null);

        var totalUnblocked = attack.Results
            .Where(r => r != null)
            .Sum(r => r.UnblockedDamage);

        if (totalUnblocked > 0)
        {
            await CreatureCmd.Heal(Creature, totalUnblocked);
        }
    }

    private async Task Stunned(IReadOnlyList<Creature> targets)
    {
        // Stunned — does nothing, next move is Fell
        await Cmd.Wait(0.5f);
    }

    // Called when Plated Armor is fully broken
    public async Task OnArmorBreak()
    {
        await HopAnimation.Play(Creature);
        await Cmd.Wait(0.3f);
        await HopAnimation.Play(Creature);
        await Cmd.Wait(0.3f);
        await HopAnimation.Play(Creature);

        SetMoveImmediate(_stunnedState, true);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var bite = new AnimState("Attack");
        var hit = new AnimState("Hit");

        bite.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Bite", bite);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}