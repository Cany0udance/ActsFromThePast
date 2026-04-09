using ActsFromThePast.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SlimeBoss : MonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);
    
    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 35);
    private int SlimedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    
    protected override string VisualsPath => "res://ActsFromThePast/monsters/slime_boss/slime_boss.tscn";
    public override bool HasDeathSfx => false;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;
    
    private const string SLAM = "SLAM";
    private const string PREP_SLAM = "PREP_SLAM";
    private const string SPLIT = "SPLIT";
    private const string GOOP_SPRAY = "GOOP_SPRAY";
    
    private bool _splitTriggered;
    public bool SplitTriggered
    {
        get => _splitTriggered;
        set
        {
            AssertMutable();
            _splitTriggered = value;
        }
    }
    
    private MoveState _splitState;
    public MoveState SplitState => _splitState;
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SplitPower>(Creature, 1m, Creature, null);
        Creature.Died += OnDeath;
    }
    
    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        ModAudio.Play("slime_boss", "death");
      //  NGame.Instance?.ScreenShake(ShakeStrength.TooMuch, ShakeDuration.Long);
    }
    
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();
        
        var goopSprayState = new MoveState(
            GOOP_SPRAY,
            GoopSpray,
            new AbstractIntent[] { new StatusIntent(SlimedCount) }
        );
        
        var prepSlamState = new MoveState(
            PREP_SLAM,
            PrepSlam,
            new AbstractIntent[] { new UnknownIntent() }
        );
        
        var slamState = new MoveState(
            SLAM,
            Slam,
            new AbstractIntent[] { new SingleAttackIntent(SlamDamage) }
        );
        
        _splitState = new MoveState(
            SPLIT,
            Split,
            new AbstractIntent[] { new UnknownIntent() }
        );
        
        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);
        
        goopSprayState.FollowUpState = prepSlamState;
        prepSlamState.FollowUpState = slamState;
        slamState.FollowUpState = moveBranch;
        _splitState.FollowUpState = _splitState;
        
        states.Add(goopSprayState);
        states.Add(prepSlamState);
        states.Add(slamState);
        states.Add(_splitState);
        states.Add(moveBranch);
        
        return new MonsterMoveStateMachine(states, goopSprayState);
    }
    
    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (SplitTriggered)
        {
            return SPLIT;
        }
        return GOOP_SPRAY;
    }
    
    private async Task GoopSpray(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        ModAudio.Play("general", "slime_attack");

        try
        {
            ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, false);
        }
        finally
        {
            ClassicSlimedTracker.CreatingClassicSlimed = false;
        }
    }
    
    private async Task PrepSlam(IReadOnlyList<Creature> targets)
    {
        PlayPrepSfx();
        TalkCmd.Play(L10NMonsterLookup("SLIME_BOSS.moves.PREP_SLAM.banter"), Creature, VfxColor.Green, VfxDuration.Long);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Long);
        await Cmd.Wait(0.3f);
    }
    
    private async Task Slam(IReadOnlyList<Creature> targets)
    {
        await JumpAnimation.Play(Creature);
        
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (targetNode != null)
            {
              //  VfxCmd.Play("vfx/vfx_weighty_impact_green", targetNode.Position);
            }
        }
        
        await Cmd.Wait(0.4f);
        
        await DamageCmd.Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }
    
    private async Task Split(IReadOnlyList<Creature> targets)
    {
        var currentHp = Creature.CurrentHp;
        var combatState = Creature.CombatState;
        var originalPosition = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Position ?? Vector2.Zero;

        _ = ShakeAnimation.Play(Creature, 1.0f, 3.0f);
        await Cmd.Wait(1.0f);
        ModAudio.Play("general", "slime_split");
        await CreatureCmd.Kill(Creature);

        var positionQueue = new Queue<Vector2>();
        var enemyContainer = NCombatRoom.Instance?.GetNode<Godot.Control>("%EnemyContainer");

        void OnChildEntered(Node child)
        {
            if (child is NCreature nc && positionQueue.Count > 0)
                nc.Position = positionQueue.Dequeue();
        }

        enemyContainer?.Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(OnChildEntered));

        positionQueue.Enqueue(originalPosition + new Vector2(-385f, 20f));
        var spikeSlime = (SpikeSlimeLarge)ModelDb.Monster<SpikeSlimeLarge>().ToMutable();
        spikeSlime.OverrideHp = currentHp;
        var spikeCreature = await CreatureCmd.Add(spikeSlime, combatState, CombatSide.Enemy, null);
        await CreatureCmd.SetMaxHp(spikeCreature, currentHp);
        await CreatureCmd.Heal(spikeCreature, currentHp);

        positionQueue.Enqueue(originalPosition + new Vector2(120f, 20f));
        var acidSlime = (AcidSlimeLarge)ModelDb.Monster<AcidSlimeLarge>().ToMutable();
        acidSlime.OverrideHp = currentHp;
        var acidCreature = await CreatureCmd.Add(acidSlime, combatState, CombatSide.Enemy, null);
        await CreatureCmd.SetMaxHp(acidCreature, currentHp);
        await CreatureCmd.Heal(acidCreature, currentHp);

        enemyContainer?.Disconnect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(OnChildEntered));
    }
    
    private void PlayPrepSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slime_boss_talk_1",
            _ => "slime_boss_talk_2"
        };
        ModAudio.Play("slime_boss", sfxName);
    }
    
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}