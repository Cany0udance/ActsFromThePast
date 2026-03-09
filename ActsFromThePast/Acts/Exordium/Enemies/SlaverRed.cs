using ActsFromThePast.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SlaverRed : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 50);

    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);
    private int ScrapeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/slaver_red/slaver_red.tscn";
    public override bool HasDeathSfx => false;

    private const string STAB = "STAB";
    private const string ENTANGLE = "ENTANGLE";
    private const string SCRAPE = "SCRAPE";

    private bool _usedEntangle;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slaver_red_death_1",
            _ => "slaver_red_death_2"
        };
        ModAudio.Play("slaver_red", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var stabState = new MoveState(
            STAB,
            Stab,
            new AbstractIntent[] { new SingleAttackIntent(StabDamage) }
        );

        var entangleState = new MoveState(
            ENTANGLE,
            Entangle,
            new AbstractIntent[] { new DebuffIntent() }
        );

        var scrapeState = new MoveState(
            SCRAPE,
            Scrape,
            new AbstractIntent[] { new SingleAttackIntent(ScrapeDamage), new DebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        stabState.FollowUpState = moveBranch;
        entangleState.FollowUpState = moveBranch;
        scrapeState.FollowUpState = moveBranch;

        states.Add(stabState);
        states.Add(entangleState);
        states.Add(scrapeState);
        states.Add(moveBranch);

        // Always starts with Stab
        return new MonsterMoveStateMachine(states, stabState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        // 25% chance to Entangle if not used yet
        if (num >= 75 && !_usedEntangle)
        {
            return ENTANGLE;
        }

        // 20% chance to Stab if Entangle already used and haven't stabbed twice in a row
        if (num >= 55 && _usedEntangle && !LastTwoMoves(stateMachine, STAB))
        {
            return STAB;
        }

        // Otherwise Scrape if not used last turn (A17 behavior)
        if (!LastMove(stateMachine, SCRAPE))
        {
            return SCRAPE;
        }

        return STAB;
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
        PlayAttackSfx();
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(StabDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "slash_attack.mp3")
            .Execute(null);
    }

    private async Task Entangle(IReadOnlyList<Creature> targets)
    {
        PlayAttackSfx();

        // Trigger net-throwing animation
        await CreatureCmd.TriggerAnim(Creature, "UseNet", 0.0f);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            
            if (creatureNode != null && targetNode != null)
            {
                var startPos = creatureNode.VfxSpawnPosition + new Vector2(-70f, -10f);
                var endPos = targetNode.VfxSpawnPosition;
                
                var effect = EntangleEffect.Create(startPos, endPos);
                NCombatRoom.Instance.CombatVfxContainer.AddChild(effect);
            }
            
            await Cmd.Wait(0.2f);
            await PowerCmd.Apply<EntangledPower>(target, 1m, Creature, null);
        }

        _usedEntangle = true;
    }

    private async Task Scrape(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(ScrapeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "slash_attack.mp3")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(target, VulnerableAmount, Creature, null);
        }
    }

    private void PlayAttackSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slaver_red_talk_1",
            _ => "slaver_red_talk_2"
        };
        ModAudio.Play("slaver_red", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var idleNoNet = new AnimState("idleNoNet", true);

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("UseNet", idleNoNet);

        return animator;
    }
}