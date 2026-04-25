using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Chosen : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 98, 95);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 103, 99);

    private int ZapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);
    private int DebilitateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private int PokeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private const int DebilitateVuln = 2;
    private const int DrainStrength = 3;
    private const int DrainWeak = 3;
    private const int HexAmount = 1;
    private const int PokeHits = 2;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/chosen/chosen.tscn";

    private static readonly LocString _hexDialog = L10NMonsterLookup("ACTSFROMTHEPAST-CHOSEN.moves.HEX.dialog");

    private const string ZAP = "ZAP";
    private const string DRAIN = "DRAIN";
    private const string DEBILITATE = "DEBILITATE";
    private const string HEX = "HEX";
    private const string POKE = "POKE";

    private bool _usedHex;

    private bool UsedHex
    {
        get => _usedHex;
        set
        {
            AssertMutable();
            _usedHex = value;
        }
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);

        if (creature != Creature)
            return;

        ModAudio.Play("chosen", "chosen_death");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var hexState = new MoveState(
            HEX,
            HexMove,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var debilitateState = new MoveState(
            DEBILITATE,
            DebilitateMove,
            new AbstractIntent[] { new SingleAttackIntent(DebilitateDamage), new DebuffIntent() }
        );

        var drainState = new MoveState(
            DRAIN,
            DrainMove,
            new AbstractIntent[] { new DebuffIntent(), new BuffIntent() }
        );

        var zapState = new MoveState(
            ZAP,
            ZapMove,
            new AbstractIntent[] { new SingleAttackIntent(ZapDamage) }
        );

        var pokeState = new MoveState(
            POKE,
            PokeMove,
            new AbstractIntent[] { new MultiAttackIntent(PokeDamage, PokeHits) }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        hexState.FollowUpState = moveBranch;
        debilitateState.FollowUpState = moveBranch;
        drainState.FollowUpState = moveBranch;
        zapState.FollowUpState = moveBranch;
        pokeState.FollowUpState = moveBranch;

        states.Add(hexState);
        states.Add(debilitateState);
        states.Add(drainState);
        states.Add(zapState);
        states.Add(pokeState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (!UsedHex)
        {
            UsedHex = true;
            return HEX;
        }

        if (!LastMove(stateMachine, DEBILITATE) && !LastMove(stateMachine, DRAIN))
        {
            return rng.NextInt(100) < 50 ? DEBILITATE : DRAIN;
        }

        return rng.NextInt(100) < 40 ? ZAP : POKE;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task HexMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_hexDialog, Creature, VfxColor.Blue, VfxDuration.Long);
        await CreatureCmd.TriggerAnim(Creature, "Hex", 0.0f);
        await Cmd.Wait(0.2f);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<HexOriginalPower>(new ThrowingPlayerChoiceContext(), target, HexAmount, Creature, null);
        }
    }

    private async Task ZapMove(IReadOnlyList<Creature> targets)
    {
        await ShakeAnimation.Play(Creature, awaitDuration: 0.5f, totalDuration: 0.3f);

        await DamageCmd.Attack(ZapDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task DrainMove(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, DrainWeak, Creature, null);
        }
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, DrainStrength, Creature, null);
    }

    private async Task DebilitateMove(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(DebilitateDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), target, DebilitateVuln, Creature, null);
        }
    }

    private async Task PokeMove(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        for (int i = 0; i < PokeHits; i++)
        {
            await DamageCmd.Attack(PokeDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var hex = new AnimState("Attack");
        var hit = new AnimState("Hit");

        hex.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hex", hex);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);
        
        return animator;
    }
}