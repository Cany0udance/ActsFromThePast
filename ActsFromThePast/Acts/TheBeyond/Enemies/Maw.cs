using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Maw : MonsterModel
{
    public override int MinInitialHp => 300;
    public override int MaxInitialHp => 300;

    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    private const int NomDamage = 5;
    private int StrUp => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    private int TerrifyDuration => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/maw/maw.tscn";

    private const string ROAR = "ROAR";
    private const string SLAM = "SLAM";
    private const string DROOL = "DROOL";
    private const string NOMNOMNOM_SINGLE = "NOMNOMNOM_SINGLE";
    private const string NOMNOMNOM_MULTI = "NOMNOMNOM_MULTI";

    private static readonly LocString _roarDialog = L10NMonsterLookup("MAW.moves.ROAR.dialog");

    private bool _roared = false;
    private int _turnCount = 1;

    private bool Roared
    {
        get => _roared;
        set { AssertMutable(); _roared = value; }
    }

    private int TurnCount
    {
        get => _turnCount;
        set { AssertMutable(); _turnCount = value; }
    }

    private int NomHitCount => TurnCount / 2;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var roarState = new MoveState(
            ROAR,
            Roar,
            new AbstractIntent[] { new DebuffIntent() }
        );
        var slamState = new MoveState(
            SLAM,
            Slam,
            new AbstractIntent[] { new SingleAttackIntent(SlamDamage) }
        );
        var droolState = new MoveState(
            DROOL,
            Drool,
            new AbstractIntent[] { new BuffIntent() }
        );
        var nomSingleState = new MoveState(
            NOMNOMNOM_SINGLE,
            NomNomNom,
            new SingleAttackIntent(NomDamage)
        );
        var nomMultiState = new MoveState(
            NOMNOMNOM_MULTI,
            NomNomNom,
            new DynamicMultiAttackIntent(() => NomDamage, () => NomHitCount)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        roarState.FollowUpState = moveBranch;
        slamState.FollowUpState = moveBranch;
        droolState.FollowUpState = moveBranch;
        nomSingleState.FollowUpState = moveBranch;
        nomMultiState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { roarState, slamState, droolState, nomSingleState, nomMultiState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        TurnCount++;

        if (!Roared)
            return ROAR;

        int num = rng.NextInt(100);
        bool lastMoveWasNom = LastMove(stateMachine, NOMNOMNOM_SINGLE) || LastMove(stateMachine, NOMNOMNOM_MULTI);

        if (num < 50 && !lastMoveWasNom)
            return NomHitCount <= 1 ? NOMNOMNOM_SINGLE : NOMNOMNOM_MULTI;

        if (!LastMove(stateMachine, SLAM) && !lastMoveWasNom)
            return SLAM;

        return DROOL;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task Roar(IReadOnlyList<Creature> targets)
    {
        ModAudio.Play("maw", "maw_death", pitchVariation: 0.1f);
        TalkCmd.Play(_roarDialog, Creature, 2.0);
        await Cmd.Wait(0.05f);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(target, TerrifyDuration, Creature, null);
            await PowerCmd.Apply<FrailPower>(target, TerrifyDuration, Creature, null);
        }
        Roared = true;
    }

    private async Task Slam(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Drool(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(Creature, StrUp, Creature, null);
    }

    private async Task NomNomNom(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        int hits = Math.Max(1, NomHitCount);
        for (int i = 0; i < hits; i++)
        {
            var target = targets.FirstOrDefault(t => t.IsAlive);
            if (target != null)
            {
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var biteEffect = BiteEffect.Create(targetNode.VfxSpawnPosition);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(biteEffect);
                }
            }
            await DamageCmd.Attack(NomDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        ModAudio.Play("maw", "maw_death");
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        return new CreatureAnimator(idle, controller);
    }
}