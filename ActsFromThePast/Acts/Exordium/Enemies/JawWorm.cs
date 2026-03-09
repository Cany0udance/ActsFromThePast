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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class JawWorm : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 40);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);
    
    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);
    private int ThrashDamage => 7;
    private int ThrashBlock => 5;
    private int BellowStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    private int BellowBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 6);
    
    public bool HardMode { get; set; } = false;
    
    protected override string VisualsPath => "res://ActsFromThePast/monsters/jaw_worm/jaw_worm.tscn";
    
    private const string CHOMP = "CHOMP";
    private const string BELLOW = "BELLOW";
    private const string THRASH = "THRASH";
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    
        if (HardMode)
        {
            await PowerCmd.Apply<StrengthPower>(Creature, BellowStrength, Creature, null);
            await CreatureCmd.GainBlock(Creature, BellowBlock, ValueProp.Move, null);
        }
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        ModAudio.Play("jaw_worm", "jaw_worm_death");
    }
    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();
        
        var chompState = new MoveState(
            CHOMP,
            Chomp,
            new AbstractIntent[] { new SingleAttackIntent(ChompDamage) }
        );
        var bellowState = new MoveState(
            BELLOW,
            Bellow,
            new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
        );
        var thrashState = new MoveState(
            THRASH,
            Thrash,
            new AbstractIntent[] { new SingleAttackIntent(ThrashDamage), new DefendIntent() }
        );
        
        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
        
        chompState.FollowUpState = moveBranch;
        bellowState.FollowUpState = moveBranch;
        thrashState.FollowUpState = moveBranch;
        
        states.Add(chompState);
        states.Add(bellowState);
        states.Add(thrashState);
        states.Add(moveBranch);
        
        // Hard mode skips guaranteed first Chomp
        var initialState = HardMode ? (MonsterState)moveBranch : chompState;
        
        return new MonsterMoveStateMachine(states, initialState);
    }
    
    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);
        
        if (num < 25)
        {
            // 25% chance: Chomp, unless last move was Chomp
            if (LastMove(stateMachine, CHOMP))
            {
                // 56.25% Bellow, 43.75% Thrash
                return rng.NextFloat() < 0.5625f ? BELLOW : THRASH;
            }
            return CHOMP;
        }
        else if (num < 55)
        {
            // 30% chance: Thrash, unless last two moves were Thrash
            if (LastTwoMoves(stateMachine, THRASH))
            {
                // 35.7% Chomp, 64.3% Bellow
                return rng.NextFloat() < 0.357f ? CHOMP : BELLOW;
            }
            return THRASH;
        }
        else
        {
            // 45% chance: Bellow, unless last move was Bellow
            if (LastMove(stateMachine, BELLOW))
            {
                // 41.6% Chomp, 58.4% Thrash
                return rng.NextFloat() < 0.416f ? CHOMP : THRASH;
            }
            return BELLOW;
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
    
    private async Task Chomp(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "chomp", 0.0f);
        
        foreach (var target in targets)
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
        
        await DamageCmd.Attack(ChompDamage)
            .FromMonster(this)
            .Execute(null);
    }
    
    private async Task Bellow(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "tailslam", 0.0f);
        ModAudio.Play(Creature, "jaw_worm", "jaw_worm_bellow");
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
        await Cmd.Wait(0.5f);
        
        await PowerCmd.Apply<StrengthPower>(Creature, BellowStrength, Creature, null);
        await CreatureCmd.GainBlock(Creature, BellowBlock, ValueProp.Move, null);
    }
    
    private async Task Thrash(IReadOnlyList<Creature> targets)
    {
        await HopAnimation.Play(Creature);
        
        await DamageCmd.Attack(ThrashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        
        await CreatureCmd.GainBlock(Creature, ThrashBlock, ValueProp.Move, null);
    }
    
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var chomp = new AnimState("chomp");
        var tailslam = new AnimState("tailslam");

        chomp.NextState = idle;
        tailslam.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("chomp", chomp);
        animator.AddAnyState("tailslam", tailslam);

        return animator;
    }
}