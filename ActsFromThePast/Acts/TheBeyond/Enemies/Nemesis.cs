using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Nemesis : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 185);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 185);

    private int FireDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private const int ScytheDamage = 45;
    private const int FireTimes = 3;
    private const int BurnAmount = 5;
    private const int ScytheCooldownTurns = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/nemesis/nemesis.tscn";

    private const string TRI_ATTACK = "TRI_ATTACK";
    private const string SCYTHE = "SCYTHE";
    private const string TRI_BURN = "TRI_BURN";

    private bool _firstMove = true;
    private int _scytheCooldown = 0;
    
    private bool _alive = true;
    private SceneTreeTimer _fireTimer;

    private bool FirstMove
    {
        get => _firstMove;
        set { AssertMutable(); _firstMove = value; }
    }

    private int ScytheCooldown
    {
        get => _scytheCooldown;
        set { AssertMutable(); _scytheCooldown = value; }
    }
    
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

    var eye0 = skeleton.GetType().GetMethod("FindBone")?.Invoke(skeleton, new object[] { "eye0" });
    var eye1 = skeleton.GetType().GetMethod("FindBone")?.Invoke(skeleton, new object[] { "eye1" });
    var eye2 = skeleton.GetType().GetMethod("FindBone")?.Invoke(skeleton, new object[] { "eye2" });

    var spineBone0 = eye0?.GetType().GetProperty("BoundObject")?.GetValue(eye0) as GodotObject;
    var spineBone1 = eye1?.GetType().GetProperty("BoundObject")?.GetValue(eye1) as GodotObject;
    var spineBone2 = eye2?.GetType().GetProperty("BoundObject")?.GetValue(eye2) as GodotObject;

    var bones = new[] { spineBone0, spineBone1, spineBone2 }
        .Where(b => b != null)
        .ToArray();

    if (bones.Length == 0) return;

    var tree = Engine.GetMainLoop() as SceneTree;
    if (tree == null) return;

    SpawnFireParticles(creatureNode, bones, tree);
}

private void SpawnFireParticles(object creatureNode, GodotObject[] bones, SceneTree tree)
{
    if (!_alive) return;
    try
    {
        var globalPos = ((dynamic)creatureNode).GlobalPosition;
        foreach (var bone in bones)
        {
            var boneX = (float)bone.Call("get_world_x");
            var boneY = (float)bone.Call("get_world_y");
            var firePos = new Vector2(
                globalPos.X + boneX * 1.1f + 10f,
                globalPos.Y + boneY * 1.1f + 10f
            );
            var effect = NemesisFireParticle.Create(firePos);
            NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(effect);
        }
    }
    catch (Exception e)
    {
        Log.Info($"[Nemesis] Fire particle error: {e.Message}");
    }

    _fireTimer = tree.CreateTimer(0.05f);
    _fireTimer.Connect("timeout", Callable.From(() => SpawnFireParticles(creatureNode, bones, tree)));
}


    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var triAttackState = new MoveState(
            TRI_ATTACK,
            TriAttack,
            new AbstractIntent[] { new MultiAttackIntent(FireDamage, FireTimes) }
        );
        var scytheState = new MoveState(
            SCYTHE,
            Scythe,
            new AbstractIntent[] { new SingleAttackIntent(ScytheDamage) }
        );
        var triBurnState = new MoveState(
            TRI_BURN,
            TriBurn,
            new AbstractIntent[] { new StatusIntent(5) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        triAttackState.FollowUpState = moveBranch;
        scytheState.FollowUpState = moveBranch;
        triBurnState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { triAttackState, scytheState, triBurnState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        ScytheCooldown--;

        if (FirstMove)
        {
            FirstMove = false;
            return rng.NextInt(100) < 50 ? TRI_ATTACK : TRI_BURN;
        }

        int num = rng.NextInt(100);

        if (num < 30)
        {
            if (!LastMove(stateMachine, SCYTHE) && ScytheCooldown <= 0)
            {
                ScytheCooldown = ScytheCooldownTurns;
                return SCYTHE;
            }
            if (rng.NextFloat() < 0.5f)
            {
                if (!LastTwoMoves(stateMachine, TRI_ATTACK))
                    return TRI_ATTACK;
                return TRI_BURN;
            }
            if (!LastMove(stateMachine, TRI_BURN))
                return TRI_BURN;
            return TRI_ATTACK;
        }
        else if (num < 65)
        {
            if (!LastTwoMoves(stateMachine, TRI_ATTACK))
                return TRI_ATTACK;
            if (rng.NextFloat() < 0.5f)
            {
                if (ScytheCooldown <= 0)
                {
                    ScytheCooldown = ScytheCooldownTurns;
                    return SCYTHE;
                }
                return TRI_BURN;
            }
            return TRI_BURN;
        }
        else
        {
            if (!LastMove(stateMachine, TRI_BURN))
                return TRI_BURN;
            if (rng.NextFloat() < 0.5f && ScytheCooldown <= 0)
            {
                ScytheCooldown = ScytheCooldownTurns;
                return SCYTHE;
            }
            return TRI_ATTACK;
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

    private async Task TriAttack(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < FireTimes; i++)
        {
            await DamageCmd.Attack(FireDamage)
                .FromMonster(this)
                .WithAttackerFx(sfx: "event:/sfx/characters/necrobinder/necrobinder_attack")
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
        await ApplyIntangibleIfNeeded();
    }

    private async Task Scythe(IReadOnlyList<Creature> targets)
    {
        PlayScytheSfx();
        await CreatureCmd.TriggerAnim(Creature, "Slash", 0.4f);
        await DamageCmd.Attack(ScytheDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/necrobinder/necrobinder_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await ApplyIntangibleIfNeeded();
    }

    private async Task TriBurn(IReadOnlyList<Creature> targets)
    {
        ModAudio.Play("nemesis", "nemesis_talk_3");
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
            ShockWaveEffect.PlayChaotic(creatureNode.VfxSpawnPosition);
        await Cmd.Wait(1.5f);

        var player = Creature.CombatState?.Players.FirstOrDefault();
        if (player != null)
        {
            var results = new List<CardPileAddResult>();
            await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, BurnAmount, false);
            CardCmd.PreviewCardPileAdd(results, 2f);
        }

        await ApplyIntangibleIfNeeded();
    }

    private async Task ApplyIntangibleIfNeeded()
    {
        if (!Creature.HasPower<IntangiblePower>())
        {
            var intangible = await PowerCmd.Apply<IntangiblePower>(Creature, 1, Creature, null);
            if (intangible != null)
                intangible.SkipNextDurationTick = true;
        }
    }

    private void PlayScytheSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll == 0 ? "nemesis_talk_1" : "nemesis_talk_2";
        ModAudio.Play("nemesis", sfxName);
    }

    private void PlayDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll == 0 ? "nemesis_death_1" : "nemesis_death_2";
        ModAudio.Play("nemesis", sfxName);
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        PlayDeathSfx();
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Slash", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}