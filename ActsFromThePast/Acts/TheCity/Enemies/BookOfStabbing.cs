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
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class BookOfStabbing : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 168, 160);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 172, 164);
    
    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int BigStabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 21);
    
    protected override string VisualsPath => "res://ActsFromThePast/monsters/book_of_stabbing/book_of_stabbing.tscn";
    
    private const string STAB = "STAB";
    private const string BIG_STAB = "BIG_STAB";
    
    private int _stabCount;
    
    private int StabCount
    {
        get => _stabCount;
        set
        {
            AssertMutable();
            _stabCount = value;
        }
    }
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _stabCount = 1;
        await PowerCmd.Apply<PainfulStabsPower>(Creature, 1, Creature, null);
        Creature.Died += OnDeath;
    }
    
    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        ModAudio.Play("book_of_stabbing", "book_of_stabbing_death");
    }
    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();
        
        var stabState = new MoveState(
            STAB,
            Stab,
            new DynamicMultiAttackIntent(() => StabDamage, () => StabCount)
        );
        
        var bigStabState = new MoveState(
            BIG_STAB,
            BigStab,
            new SingleAttackIntent(BigStabDamage)
        );
        
        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
        
        stabState.FollowUpState = moveBranch;
        bigStabState.FollowUpState = moveBranch;
        
        states.Add(stabState);
        states.Add(bigStabState);
        states.Add(moveBranch);
        
        return new MonsterMoveStateMachine(states, moveBranch);
    }
    
    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);
    
        if (num < 15)
        {
            if (LastMove(stateMachine, BIG_STAB))
            {
                StabCount++;
                return STAB;
            }
            else
            {
                StabCount++; // A18+ behavior
                return BIG_STAB;
            }
        }
        else
        {
            if (LastTwoMoves(stateMachine, STAB))
            {
                StabCount++; // A18+ behavior
                return BIG_STAB;
            }
            else
            {
                StabCount++;
                return STAB;
            }
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
    
    private async Task Stab(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StabDamage)
            .FromMonster(this)
            .WithAttackerAnim("Stab", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        
        for (int i = 1; i < StabCount; i++)
        {
            PlayStabSfx();
            await DamageCmd.Attack(StabDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }
    
    private void PlayStabSfx()
    {
        var roll = Rng.Chaotic.NextInt(4);
        var sfxName = $"book_of_stabbing_attack_{roll}";
        ModAudio.Play("book_of_stabbing", sfxName);
    }
    
    private async Task BigStab(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigStabDamage)
            .FromMonster(this)
            .WithAttackerAnim("BigStab", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
    
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var stab = new AnimState("Attack");
        var bigStab = new AnimState("Attack_2");
        var hit = new AnimState("Hit");
        
        stab.NextState = idle;
        bigStab.NextState = idle;
        hit.NextState = idle;
        
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Stab", stab);
        animator.AddAnyState("BigStab", bigStab);
        animator.AddAnyState("Hit", hit);
        
        return animator;
    }
}