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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Reptomancer : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 180);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 190);

    private int SnakeStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 13);
    private int BigBiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 34, 30);
    private const int SnakeStrikeHits = 2;
    private const int DaggersPerSpawn = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/reptomancer/reptomancer.tscn";

    private const string SNAKE_STRIKE = "SNAKE_STRIKE";
    private const string SPAWN_DAGGER = "SPAWN_DAGGER";
    private const string BIG_BITE = "BIG_BITE";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        foreach (var teammate in CombatState.GetTeammatesOf(Creature).Where(t => t != Creature))
        {
            await PowerCmd.Apply<MinionPower>(teammate, 1, Creature, null);
        }
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);

        if (creature != Creature)
            return;

        foreach (var teammate in CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive))
        {
            await CreatureCmd.Kill(teammate);
        }
    }

    private int NumAliveDaggers()
    {
        return CombatState.GetTeammatesOf(Creature)
            .Count(t => t != Creature && t.IsAlive);
    }

    private bool CanSpawn()
    {
        return NumAliveDaggers() < 4;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var spawnDaggerState = new MoveState(
            SPAWN_DAGGER,
            SpawnDagger,
            new AbstractIntent[] { new SummonIntent() }
        );

        var snakeStrikeState = new MoveState(
            SNAKE_STRIKE,
            SnakeStrike,
            new AbstractIntent[] { new MultiAttackIntent(SnakeStrikeDamage, SnakeStrikeHits), new DebuffIntent() }
        );

        var bigBiteState = new MoveState(
            BIG_BITE,
            BigBite,
            new SingleAttackIntent(BigBiteDamage)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        spawnDaggerState.FollowUpState = moveBranch;
        snakeStrikeState.FollowUpState = moveBranch;
        bigBiteState.FollowUpState = moveBranch;

        states.Add(spawnDaggerState);
        states.Add(snakeStrikeState);
        states.Add(bigBiteState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, spawnDaggerState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 33)
        {
            if (!LastMove(stateMachine, SNAKE_STRIKE))
                return SNAKE_STRIKE;
            return SelectFromReroll(rng, stateMachine, 33, 99);
        }
        else if (num < 66)
        {
            if (!LastTwoMoves(stateMachine, SPAWN_DAGGER) && CanSpawn())
                return SPAWN_DAGGER;
            return SNAKE_STRIKE;
        }
        else
        {
            if (!LastMove(stateMachine, BIG_BITE))
                return BIG_BITE;
            return SelectFromReroll(rng, stateMachine, 0, 65);
        }
    }

    private string SelectFromReroll(Rng rng, MonsterMoveStateMachine stateMachine, int min, int max)
    {
        int num = rng.NextInt(max - min + 1) + min;

        if (num < 33)
        {
            if (!LastMove(stateMachine, SNAKE_STRIKE))
                return SNAKE_STRIKE;
            return SelectFromReroll(rng, stateMachine, 33, 99);
        }
        else if (num < 66)
        {
            if (!LastTwoMoves(stateMachine, SPAWN_DAGGER) && CanSpawn())
                return SPAWN_DAGGER;
            return SNAKE_STRIKE;
        }
        else
        {
            if (!LastMove(stateMachine, BIG_BITE))
                return BIG_BITE;
            return SelectFromReroll(rng, stateMachine, 0, 65);
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

    private async Task SpawnDagger(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Summon", 0.0f);
        await Cmd.Wait(0.5f);

        var occupiedSlots = CombatState.GetTeammatesOf(Creature)
            .Where(t => t.IsAlive)
            .Select(t => t.SlotName)
            .ToHashSet();

        int daggersSpawned = 0;
        foreach (var slot in CombatState.Encounter.Slots.Where(s => s != "reptomancer"))
        {
            if (daggersSpawned >= DaggersPerSpawn)
                break;

            if (occupiedSlots.Contains(slot))
                continue;

            var summoned = await CreatureCmd.Add<SnakeDagger>(CombatState, slot);
            if (summoned != null)
            {
                occupiedSlots.Add(slot);
                await PowerCmd.Apply<MinionPower>(summoned, 1, Creature, null);
                daggersSpawned++;
            }
        }
    }

    private async Task SnakeStrike(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Strike", 0.0f);
        await Cmd.Wait(0.3f);

        for (int i = 0; i < SnakeStrikeHits; i++)
        {
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

            await Cmd.Wait(0.1f);

            await DamageCmd.Attack(SnakeStrikeDamage)
                .FromMonster(this)
                .Execute(null);
        }

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, 1, Creature, null);
        }
    }

    private async Task BigBite(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

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

        await Cmd.Wait(0.1f);

        await DamageCmd.Attack(BigBiteDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var summon = new AnimState("Sumon");
        var hurt = new AnimState("Hurt");

        attack.NextState = idle;
        summon.NextState = idle;
        hurt.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Strike", attack);
        animator.AddAnyState("Summon", summon);
        animator.AddAnyState("Hit", hurt);

        return animator;
    }
}