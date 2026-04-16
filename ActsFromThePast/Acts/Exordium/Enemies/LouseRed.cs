using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast;

public sealed class LouseRed : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 16, 15);

    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private bool _isOpen = true;
    private readonly Dictionary<Creature, int> _biteDamageByCreature = new();

    private int GetBiteDamage()
    {
        return _biteDamageByCreature.TryGetValue(Creature, out var d) ? d : 0;
    }

    protected override string VisualsPath => "res://ActsFromThePast/monsters/louse_red/louse_red.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Insect;

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            AssertMutable();
            _isOpen = value;
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var biteState = new MoveState(
            "BITE",
            Bite,
            new AbstractIntent[] { new DynamicSingleAttackIntent(() => GetBiteDamage()) }
        );
        var growState = new MoveState(
            "GROW",
            Grow,
            new AbstractIntent[] { new BuffIntent() }
        );

        var randomBranch = new RandomBranchState("RANDOM");

        biteState.FollowUpState = randomBranch;
        growState.FollowUpState = randomBranch;

        var growMaxRepeats = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies) ? 1 : 2;

        randomBranch.AddBranch(growState, growMaxRepeats, 25f);
        randomBranch.AddBranch(biteState, 2, 75f);

        states.Add(biteState);
        states.Add(growState);
        states.Add(randomBranch);

        return new MonsterMoveStateMachine(states, randomBranch);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        var dmg = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies)
            ? RunRng.MonsterAi.NextInt(6, 9)
            : RunRng.MonsterAi.NextInt(5, 8);
        _biteDamageByCreature[Creature] = dmg;

        var curlUpAmount = AscensionHelper.HasAscension(AscensionLevel.ToughEnemies)
            ? RunRng.MonsterAi.NextInt(9, 13)
            : RunRng.MonsterAi.NextInt(3, 8);

        await PowerCmd.Apply<CurlUpPower>(Creature, curlUpAmount, Creature, null);
    }

    private async Task Bite(IReadOnlyList<Creature> targets)
    {
        if (!_isOpen)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(Creature, "transitiontoopened", 0.0f);
            await Cmd.Wait(0.5f);
            _isOpen = true;
        }
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(GetBiteDamage())
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_attack")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task Grow(IReadOnlyList<Creature> targets)
    {
        if (!_isOpen)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(Creature, "transitiontoopened", 0.0f);
            await Cmd.Wait(0.3f);
            await CreatureCmd.TriggerAnim(Creature, "rear", 0.0f);
            await Cmd.Wait(0.7f);
            _isOpen = true;
        }
        else
        {
            await CreatureCmd.TriggerAnim(Creature, "rear", 0.0f);
            await Cmd.Wait(0.5f);
        }
        await PowerCmd.Apply<StrengthPower>(Creature, StrengthAmount, Creature, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var idleClosed = new AnimState("idle closed", true);
        var rear = new AnimState("rear");
        var open = new AnimState("transitiontoopened");
        var close = new AnimState("transitiontoclosed");

        rear.NextState = idle;
        open.NextState = idle;
        close.NextState = idleClosed;

        idle.AddBranch("rear", rear);
        idle.AddBranch("Curl", close);
        idle.AddBranch("transitiontoclosed", close);

        idleClosed.AddBranch("transitiontoopened", open);
        idleClosed.AddBranch("rear", rear);

        open.AddBranch("rear", rear);

        return new CreatureAnimator(idle, controller);
    }
}