using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
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

namespace ActsFromThePast;

public sealed class SnakePlant : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 75);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 82, 79);

    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private const int ChompHits = 3;
    private const int DebuffAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/snake_plant/snake_plant.tscn";

    private const string CHOMP = "CHOMP";
    private const string SPORES = "SPORES";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MalleablePower>(new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var chompState = new MoveState(
            CHOMP,
            ChompMove,
            new AbstractIntent[] { new MultiAttackIntent(ChompDamage, ChompHits) }
        );

        var sporesState = new MoveState(
            SPORES,
            SporesMove,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        chompState.FollowUpState = moveBranch;
        sporesState.FollowUpState = moveBranch;

        states.Add(chompState);
        states.Add(sporesState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 65)
        {
            if (LastTwoMoves(stateMachine, CHOMP))
                return SPORES;
            return CHOMP;
        }
        else
        {
            if (!LastMove(stateMachine, SPORES) && !LastMoveBefore(stateMachine, SPORES))
                return SPORES;
            return CHOMP;
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

    private static bool LastMoveBefore(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 2].Id == moveId;
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Chomp", 0.0f);
        await Cmd.Wait(0.5f);

        for (int i = 0; i < ChompHits; i++)
        {
            foreach (var target in targets.Where(t => t.IsAlive))
            {
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var position = targetNode.VfxSpawnPosition;
                    var offsetX = Rng.Chaotic.NextFloat() * 100f - 50f;
                    var offsetY = Rng.Chaotic.NextFloat() * 100f - 50f;
                    var effect = BiteEffect.Create(position + new Vector2(offsetX, offsetY), new Color("7fff00"));
                    NCombatRoom.Instance.CombatVfxContainer.AddChild(effect);
                    effect.GlobalPosition = position + new Vector2(offsetX, offsetY);
                }
            }

            await Cmd.Wait(0.2f);

            await DamageCmd.Attack(ChompDamage)
                .FromMonster(this)
                .Execute(null);
        }
    }

    private async Task SporesMove(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, DebuffAmount, Creature, null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var chomp = new AnimState("Attack");
        var hit = new AnimState("Hit");

        chomp.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Chomp", chomp);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}