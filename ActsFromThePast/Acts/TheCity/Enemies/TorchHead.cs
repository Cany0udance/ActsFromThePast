using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public sealed class TorchHead : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 40);

    private const int AttackDamage = 7;
    private const float FireInterval = 0.04f;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/torch_head/torch_head.tscn";

    private const string TACKLE = "TACKLE";

    private SceneTreeTimer _fireTimer;
    private bool _alive = true;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
        StartFireLoop();
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        _alive = false;
    }

    private void StartFireLoop()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals?.SpineBody;
        if (spineBody == null || creatureNode == null) return;

        var skeleton = spineBody.GetType().GetMethod("GetSkeleton")?.Invoke(spineBody, null);
        if (skeleton == null) return;

        var bone = skeleton.GetType().GetMethod("FindBone")?.Invoke(skeleton, new object[] { "fireslot" });
        if (bone == null) return;

        var spineBone = bone.GetType().GetProperty("BoundObject")?.GetValue(bone) as GodotObject;
        if (spineBone == null) return;

        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null) return;

        SpawnFireParticle(creatureNode, spineBone, tree);
    }

    private void SpawnFireParticle(object creatureNode, GodotObject spineBone, SceneTree tree)
    {
        if (!_alive) return;

        try
        {
            var boneX = (float)spineBone.Call("get_world_x");
            var boneY = (float)spineBone.Call("get_world_y");

            var globalPos = ((dynamic)creatureNode).GlobalPosition;

            var firePos = new Vector2(
                globalPos.X + boneX * 1.1f,
                globalPos.Y + boneY * 1.1f
            );

            var effect = TorchHeadFireEffect.Create(firePos);
            NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(effect);
        }
        catch (Exception e)
        {
            Log.Info($"[TorchHead] Fire particle error: {e.Message}");
        }

        _fireTimer = tree.CreateTimer(FireInterval);
        _fireTimer.Connect("timeout", Callable.From(() => SpawnFireParticle(creatureNode, spineBone, tree)));
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackleState = new MoveState(
            TACKLE,
            TackleMove,
            new AbstractIntent[] { new SingleAttackIntent(AttackDamage) }
        );

        tackleState.FollowUpState = tackleState;

        states.Add(tackleState);

        return new MonsterMoveStateMachine(states, tackleState);
    }

    private async Task TackleMove(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/axe_ruby_raider/axe_ruby_raider_attack")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}