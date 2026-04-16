using ActsFromThePast.Powers;
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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class FungiBeast : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 24, 22);
    public override int MaxInitialHp => 28;

    private int BiteDamage => 6;
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    private const int VulnerableAmount = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/fungi_beast/fungi_beast.tscn";

    private const string BITE = "BITE";
    private const string GROW = "GROW";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SporeCloudPower>(Creature, VulnerableAmount, Creature, null);
    //    Creature.Died += OnDeath;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
    
        if (creature != Creature)
            return;
    
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var position = creatureNode.GetBottomOfHitbox();
        
            var tree = Engine.GetMainLoop() as SceneTree;
            tree?.CreateTimer(0.01f).Connect("timeout", Callable.From(() =>
            {
                var vfx = NSporeImpactVfx.Create(position, new Color("8aad7d"));
                if (vfx != null)
                {
                    tree?.Root.AddChild(vfx);
                }
            }));
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var biteState = new MoveState(
            BITE,
            Bite,
            new AbstractIntent[] { new SingleAttackIntent(BiteDamage) }
        );

        var growState = new MoveState(
            GROW,
            Grow,
            new AbstractIntent[] { new BuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        biteState.FollowUpState = moveBranch;
        growState.FollowUpState = moveBranch;

        states.Add(biteState);
        states.Add(growState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 60)
        {
            if (LastTwoMoves(stateMachine, BITE))
            {
                return GROW;
            }
            return BITE;
        }
        else
        {
            if (LastMove(stateMachine, GROW))
            {
                return BITE;
            }
            return GROW;
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

    private async Task Bite(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Grow(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(Creature, StrengthAmount, Creature, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");
    
        attack.NextState = idle;
        hit.NextState = idle;
    
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hit);
        
        controller.GetAnimationState().SetTimeScale(Rng.Chaotic.NextFloat(0.7f, 1.0f));
    
        return animator;
    }
}