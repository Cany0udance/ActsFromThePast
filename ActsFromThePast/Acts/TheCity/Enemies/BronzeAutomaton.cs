using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class BronzeAutomaton : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);

    private int FlailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 45);
    private int StrAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 9);

    private const int FlailHits = 2;
    private const int ArtifactAmount = 3;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/bronze_automaton/bronze_automaton.tscn";

    private const string SPAWN_ORBS = "SPAWN_ORBS";
    private const string BOOST = "BOOST";
    private const string FLAIL = "FLAIL";
    private const string HYPER_BEAM = "HYPER_BEAM";

    private int _numTurns;
    private int NumTurns
    {
        get => _numTurns;
        set
        {
            AssertMutable();
            _numTurns = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _numTurns = 0;
        await PowerCmd.Apply<ArtifactPower>(Creature, ArtifactAmount, Creature, null);
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;

        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Long);

        var livingMinions = CombatState.GetTeammatesOf(Creature)
            .Where(t => t != Creature && t.IsAlive)
            .ToList();

        foreach (var minion in livingMinions)
        {
            await CreatureCmd.Kill(minion);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var spawnOrbsState = new MoveState(
            SPAWN_ORBS,
            SpawnOrbs,
            new AbstractIntent[] { new SummonIntent() }
        );

        var boostState = new MoveState(
            BOOST,
            Boost,
            new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
        );

        var flailState = new MoveState(
            FLAIL,
            Flail,
            new AbstractIntent[] { new MultiAttackIntent(FlailDamage, FlailHits) }
        );

        var hyperBeamState = new MoveState(
            HYPER_BEAM,
            HyperBeam,
            new AbstractIntent[] { new SingleAttackIntent(BeamDamage) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        spawnOrbsState.FollowUpState = moveBranch;
        boostState.FollowUpState = moveBranch;
        flailState.FollowUpState = moveBranch;
        hyperBeamState.FollowUpState = moveBranch;

        states.Add(spawnOrbsState);
        states.Add(boostState);
        states.Add(flailState);
        states.Add(hyperBeamState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, spawnOrbsState);
    }
    
    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (NumTurns == 4)
        {
            NumTurns = 0;
            return HYPER_BEAM;
        }

        if (LastMove(stateMachine, HYPER_BEAM))
        {
            return BOOST;
        }

        NumTurns++;

        if (!LastMove(stateMachine, BOOST) && !LastMove(stateMachine, SPAWN_ORBS))
        {
            return BOOST;
        }

        return FLAIL;
    }

    private async Task SpawnOrbs(IReadOnlyList<Creature> targets)
    {
        var slots = CombatState.Encounter.Slots
            .Where(s => s.StartsWith("orb"))
            .ToList();

        int index = 0;
        var spawnTasks = new List<Task>();

        foreach (var slot in slots)
        {
            var orb = (BronzeOrb)ModelDb.Monster<BronzeOrb>().ToMutable();
            orb.BobIndex = index;
            orb.SpawnAnimPending = true;
            var summoned = await CreatureCmd.Add(orb, CombatState, CombatSide.Enemy, slot);
            await PowerCmd.Apply<MinionPower>(summoned, 1, Creature, null);
            spawnTasks.Add(BronzeOrbSpawnAnimation.Play(summoned));

            index++;
        }

        if (spawnTasks.Count > 0)
            await Task.WhenAll(spawnTasks);
    }
    
    private async Task Boost(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(Creature, StrAmount, Creature, null);
    }

    private async Task Flail(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        for (int i = 0; i < FlailHits; i++)
        {
            await DamageCmd.Attack(FlailDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
                .Execute(null);
        }
    }

    private async Task HyperBeam(IReadOnlyList<Creature> targets)
    {
        var automatonNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var target = targets.FirstOrDefault(t => t.IsAlive);
        var targetNode = target != null ? NCombatRoom.Instance?.GetCreatureNode(target) : null;

        if (automatonNode != null && targetNode != null)
        {
            var sourcePos = automatonNode.VfxSpawnPosition;
            var targetPos = targetNode.VfxSpawnPosition;

            var beam = NHyperbeamVfx.Create(sourcePos, targetPos);
            if (beam != null)
                NCombatRoom.Instance?.CombatVfxContainer?.AddChild(beam);

            var impact = NHyperbeamImpactVfx.Create(sourcePos, targetPos);
            if (impact != null)
                NCombatRoom.Instance?.CombatVfxContainer?.AddChild(impact);

            // Wait for anticipation + laser to fire before dealing damage
            await Cmd.Wait(NHyperbeamVfx.hyperbeamAnticipationDuration + NHyperbeamVfx.hyperbeamLaserDuration);
        }

        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}