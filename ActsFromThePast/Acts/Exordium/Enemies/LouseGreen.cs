using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public sealed class LouseGreen : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 11);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);


    private const int WeakAmount = 2;
    
    private bool _isOpen = true;
    private readonly Dictionary<Creature, int> _biteDamageByCreature = new();

    private int GetBiteDamage()
    {
        return _biteDamageByCreature.TryGetValue(Creature, out var d) ? d : 0;
    }

    protected override string VisualsPath => "res://ActsFromThePast/monsters/louse_green/louse_green.tscn";

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
        var spitWebState = new MoveState(
            "SPIT_WEB",
            SpitWeb,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var randomBranch = new RandomBranchState("RANDOM");

        biteState.FollowUpState = randomBranch;
        spitWebState.FollowUpState = randomBranch;

        var spitWebMaxRepeats = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies) ? 1 : 2;

        randomBranch.AddBranch(spitWebState, spitWebMaxRepeats, 25f);
        randomBranch.AddBranch(biteState, 2, 75f);

        states.Add(biteState);
        states.Add(spitWebState);
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

        await PowerCmd.Apply<CurlUpPower>(new ThrowingPlayerChoiceContext(), Creature, curlUpAmount, Creature, null);
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

    private async Task SpitWeb(IReadOnlyList<Creature> targets)
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

        ModAudio.Play("general", "attack_magic_fast_3", 0f, 0.02f, 1.9f);
    
        var combatRoom = NCombatRoom.Instance;
        var louseNode = combatRoom?.GetCreatureNode(Creature);
        if (louseNode != null)
        {
            var sourcePos = louseNode.VfxSpawnPosition + new Vector2(-70f, -10f);
    
            foreach (var target in targets.Where(t => t.IsAlive))
            {
                var targetNode = combatRoom?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var targetPos = targetNode.VfxSpawnPosition;
                    var effect = WebEffect.Create(sourcePos, targetPos);
                    combatRoom.CombatVfxContainer.AddChild(effect);
                }
            }
        }

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, WeakAmount, Creature, null);
        }
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