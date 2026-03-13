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
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class GremlinLeader : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 145, 140);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 155, 148);

    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 6);
    private const int StabDamage = 6;
    private const int StabHits = 3;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_leader/gremlin_leader.tscn";

    private static readonly LocString _encourageLine1 = L10NMonsterLookup("GREMLIN_LEADER.moves.ENCOURAGE.dialog1");
    private static readonly LocString _encourageLine2 = L10NMonsterLookup("GREMLIN_LEADER.moves.ENCOURAGE.dialog2");
    private static readonly LocString _encourageLine3 = L10NMonsterLookup("GREMLIN_LEADER.moves.ENCOURAGE.dialog3");
    private static readonly LocString _gremlinFleeeLine1 = L10NMonsterLookup("GREMLIN_LEADER.gremlinFlee1");
    private static readonly LocString _gremlinFleeLine2 = L10NMonsterLookup("GREMLIN_LEADER.gremlinFlee2");

    private const string RALLY = "RALLY";
    private const string ENCOURAGE = "ENCOURAGE";
    private const string STAB = "STAB";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        // Apply Minion power to starting gremlins
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

        // Remove MinionPower so combat doesn't end immediately
        foreach (var teammate in CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive))
        {
            await PowerCmd.Remove<MinionPower>(teammate);
        }
    }

    private int NumAliveGremlins()
    {
        return CombatState.GetTeammatesOf(Creature)
            .Count(t => t != Creature && t.IsAlive);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var rallyState = new MoveState(
            RALLY,
            Rally,
            new AbstractIntent[] { new SummonIntent() }
        );

        var encourageState = new MoveState(
            ENCOURAGE,
            Encourage,
            new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
        );

        var stabState = new MoveState(
            STAB,
            Stab,
            new AbstractIntent[] { new MultiAttackIntent(StabDamage, StabHits) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        rallyState.FollowUpState = moveBranch;
        encourageState.FollowUpState = moveBranch;
        stabState.FollowUpState = moveBranch;

        states.Add(rallyState);
        states.Add(encourageState);
        states.Add(stabState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int aliveGremlins = NumAliveGremlins();
        int num = rng.NextInt(100);

        if (aliveGremlins == 0)
        {
            if (num < 75)
            {
                if (!LastMove(stateMachine, RALLY))
                    return RALLY;
                return STAB;
            }
            if (!LastMove(stateMachine, STAB))
                return STAB;
            return RALLY;
        }

        if (aliveGremlins < 2)
        {
            if (num < 50)
            {
                if (!LastMove(stateMachine, RALLY))
                    return RALLY;
                // Re-roll in 50-99 range
                return SelectFromUpperRange(rng, stateMachine);
            }
            return SelectFromUpperRange(rng, stateMachine);
        }

        // 2+ gremlins alive
        if (num < 66)
        {
            if (!LastMove(stateMachine, ENCOURAGE))
                return ENCOURAGE;
            return STAB;
        }
        if (!LastMove(stateMachine, STAB))
            return STAB;
        return ENCOURAGE;
    }

    private string SelectFromUpperRange(Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);
        if (num < 60) // maps to 50-80 range in original (30/50 = 60%)
        {
            if (!LastMove(stateMachine, ENCOURAGE))
                return ENCOURAGE;
            return STAB;
        }
        if (!LastMove(stateMachine, STAB))
            return STAB;
        // Re-roll in 0-80 range
        int reroll = rng.NextInt(80);
        if (reroll < 50)
        {
            if (!LastMove(stateMachine, RALLY))
                return RALLY;
            return ENCOURAGE;
        }
        if (!LastMove(stateMachine, ENCOURAGE))
            return ENCOURAGE;
        return STAB;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task Rally(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Call", 0.0f);

        var occupiedSlots = CombatState.GetTeammatesOf(Creature)
            .Where(t => t.IsAlive)
            .Select(t => t.SlotName)
            .ToHashSet();

        for (int i = 0; i < 2; i++)
        {
            var emptySlot = CombatState.Encounter.Slots
                .Where(s => s != "leader" && !occupiedSlots.Contains(s))
                .LastOrDefault();

            if (emptySlot == null)
                break;

            var summoned = await SummonRandomGremlin(emptySlot);
            if (summoned != null)
            {
                occupiedSlots.Add(emptySlot);

                var node = NCombatRoom.Instance?.GetCreatureNode(summoned);
                if (node != null)
                    node.Visible = false;

                await PowerCmd.Apply<MinionPower>(summoned, 1, Creature, null);
                await SummonSlideInAnimation.Play(summoned);
            }
        }
    }

    private async Task<Creature?> SummonRandomGremlin(string slotName)
    {
        // Weighted: Mad/Sneaky/Fat are twice as likely as Shield/Wizard
        var roll = Rng.NextInt(8);
        return roll switch
        {
            0 or 1 => await CreatureCmd.Add<GremlinMad>(CombatState, slotName),
            2 or 3 => await CreatureCmd.Add<GremlinSneaky>(CombatState, slotName),
            4 or 5 => await CreatureCmd.Add<GremlinFat>(CombatState, slotName),
            6 => await CreatureCmd.Add<GremlinShield>(CombatState, slotName),
            _ => await CreatureCmd.Add<GremlinWizard>(CombatState, slotName)
        };
    }

    private async Task Encourage(IReadOnlyList<Creature> targets)
    {
        var encourageLines = new[] { _encourageLine1, _encourageLine2, _encourageLine3 };
        var line = encourageLines[Rng.Chaotic.NextInt(encourageLines.Length)];
        TalkCmd.Play(line, Creature, 2.0);

        // Strength to self and all living allies; Block to living allies only
        await PowerCmd.Apply<StrengthPower>(Creature, StrengthAmount, Creature, null);

        foreach (var teammate in CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(teammate, StrengthAmount, Creature, null);
            await CreatureCmd.GainBlock(teammate, BlockAmount, ValueProp.Move, null);
        }
    }

    private async Task Stab(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.0f);
        await Cmd.Wait(0.5f);

        for (int i = 0; i < StabHits; i++)
        {
            await DamageCmd.Attack(StabDamage)
                .FromMonster(this)
                .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var call = new AnimState("Call");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        call.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Call", call);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}