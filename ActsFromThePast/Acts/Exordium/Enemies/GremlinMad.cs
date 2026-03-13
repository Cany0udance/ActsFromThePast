using ActsFromThePast.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinMad : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 21, 20);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 25, 24);

    private int ScratchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private int AngryAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_mad/gremlin_mad.tscn";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<AngryPower>(Creature, AngryAmount, Creature, null);
        Creature.Died += OnDeath;
        GremlinLeaderHelper.SubscribeToLeaderDeath(Creature, CombatState);
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var scratchState = new MoveState(
            "SCRATCH",
            Scratch,
            new AbstractIntent[] { new SingleAttackIntent(ScratchDamage) }
        );

        scratchState.FollowUpState = scratchState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { scratchState },
            scratchState
        );
    }

    private async Task Scratch(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(ScratchDamage)
            .FromMonster(this)
            .WithHitFx(tmpSfx: "slash_attack.mp3")
            .WithHitVfxNode(target =>
            {
                var vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_scratch")).Instantiate<Node2D>();
                vfx.Scale = new Vector2(-1f, 1f);
                vfx.GlobalPosition = NCombatRoom.Instance?.GetCreatureNode(target)?.VfxSpawnPosition ?? Vector2.Zero;
                return vfx;
            })
            .Execute(null);
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "gremlin_mad_death_1",
            _ => "gremlin_mad_death_2"
        };
        ModAudio.Play("gremlin_mad", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var animator = new CreatureAnimator(idle, controller);

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        animState.Apply(controller.GetSkeleton());

        return animator;
    }
}