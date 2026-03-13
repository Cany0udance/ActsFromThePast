using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;
public sealed class Centurion : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 76);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 83, 80);

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int FuryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private const int FuryHits = 3;
    private int ProtectBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 20, 15);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/centurion/centurion.tscn";

    private const string SLASH = "SLASH";
    private const string PROTECT = "PROTECT";
    private const string FURY = "FURY";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
    }

    private void PlayAttackSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "centurion_talk_1",
            _ => "centurion_talk_2"
        };
        ModAudio.Play("centurion", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var slashState = new MoveState(
            SLASH,
            Slash,
            new AbstractIntent[] { new SingleAttackIntent(SlashDamage) }
        );
        var protectState = new MoveState(
            PROTECT,
            Protect,
            new AbstractIntent[] { new DefendIntent() }
        );
        var furyState = new MoveState(
            FURY,
            Fury,
            new AbstractIntent[] { new MultiAttackIntent(FuryDamage, FuryHits) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        slashState.FollowUpState = moveBranch;
        protectState.FollowUpState = moveBranch;
        furyState.FollowUpState = moveBranch;

        states.Add(slashState);
        states.Add(protectState);
        states.Add(furyState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);
        var teammateCount = CombatState.GetTeammatesOf(Creature).Count;
        bool hasAllies = teammateCount > 1;

        // 35% chance: Protect (if allies alive) or Fury (if alone), unless last two were that move
        if (num >= 65 && !LastTwoMoves(stateMachine, PROTECT) && !LastTwoMoves(stateMachine, FURY))
        {
            return hasAllies ? PROTECT : FURY;
        }

        // Otherwise: Slash, unless last two were Slash
        if (!LastTwoMoves(stateMachine, SLASH))
        {
            return SLASH;
        }

        // Fallback: Protect/Fury
        return hasAllies ? PROTECT : FURY;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Slash(IReadOnlyList<Creature> targets)
    {
        PlayAttackSfx();
        await CreatureCmd.TriggerAnim(Creature, "MaceSlam", 0.0f);
        await Cmd.Wait(0.3f);

        await DamageCmd.Attack((decimal)SlashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Protect(IReadOnlyList<Creature> targets)
    {
        await Cmd.Wait(0.25f);

        var teammates = CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive);
        var target = teammates.Any() ? Rng.NextItem(teammates) : Creature;
        await CreatureCmd.GainBlock(target, (decimal)ProtectBlock, ValueProp.Move, null);
    }

    private async Task Fury(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < FuryHits; i++)
        {
            PlayAttackSfx();
            await CreatureCmd.TriggerAnim(Creature, "MaceSlam", 0.0f);
            await Cmd.Wait(0.3f);

            await DamageCmd.Attack((decimal)FuryDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
                .Execute(null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // TODO fix extremely cursed animations
        var idle = new AnimState("Idle", true);
        var maceSlam = new AnimState("Attack");
        var hit = new AnimState("Hit");

        maceSlam.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("MaceSlam", maceSlam);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}