using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class OrbWalker : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 92, 90);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 102, 96);

    private int LaserDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int ClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
    private int StrengthUpAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/orb_walker/orb_walker.tscn";

    private const string LASER = "LASER";
    private const string CLAW = "CLAW";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<StrengthUpPower>(Creature, StrengthUpAmount, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var laserState = new MoveState(
            LASER,
            Laser,
            new AbstractIntent[] { new SingleAttackIntent(LaserDamage), new StatusIntent(2) }
        );

        var clawState = new MoveState(
            CLAW,
            Claw,
            new SingleAttackIntent(ClawDamage)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        laserState.FollowUpState = moveBranch;
        clawState.FollowUpState = moveBranch;

        states.Add(laserState);
        states.Add(clawState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (num < 40)
        {
            if (!LastTwoMoves(stateMachine, CLAW))
                return CLAW;
            else
                return LASER;
        }
        else
        {
            if (!LastTwoMoves(stateMachine, LASER))
                return LASER;
            else
                return CLAW;
        }
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task Laser(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Laser", 0.0f);
        await Cmd.Wait(0.4f);

        await DamageCmd.Attack(LaserDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);

        foreach (var target in targets)
        {
            var player = target.Player ?? target.PetOwner;
            var statusCards = new CardPileAddResult[2];

            var burn1 = CombatState.CreateCard<Burn>(player);
            statusCards[0] = await CardPileCmd.AddGeneratedCardToCombat(burn1, PileType.Discard, false);

            var burn2 = CombatState.CreateCard<Burn>(player);
            statusCards[1] = await CardPileCmd.AddGeneratedCardToCombat(burn2, PileType.Draw, false, CardPilePosition.Random);

            if (LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards);
                await Cmd.Wait(1f);
            }
        }
    }

    private async Task Claw(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(ClawDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Laser", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}