using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Transient : MonsterModel
{
    public override int MinInitialHp => 999;
    public override int MaxInitialHp => 999;

    private int StartingDeathDmg => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);
    private const int IncrementDmg = 10;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/transient/transient.tscn";

    private const string ATTACK = "ATTACK";

    private int _count = 0;

    private int Count
    {
        get => _count;
        set { AssertMutable(); _count = value; }
    }
    
    private Decimal _multiplayerDamageMultiplier = 1m;

    private int CurrentAttackDamage => (int)(( StartingDeathDmg + Count * IncrementDmg) * _multiplayerDamageMultiplier);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<FadingPower>(Creature, AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 6, 5), Creature, null);
        await PowerCmd.Apply<ShiftingPower>(Creature, 1, Creature, null);

        var playerCount = Creature.CombatState.Players.Count;
        if (playerCount > 1)
        {
            _multiplayerDamageMultiplier = playerCount
                                           * MultiplayerScalingModel.GetMultiplayerScaling(
                                               Creature.CombatState.Encounter,
                                               Creature.CombatState.RunState.CurrentActIndex);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackState = new MoveState(
            ATTACK,
            Attack,
            new DynamicSingleAttackIntent(() => CurrentAttackDamage)
        );

        attackState.FollowUpState = attackState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { attackState },
            attackState
        );
    }

    private async Task Attack(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Swing", 0.4f);
        await DamageCmd.Attack(CurrentAttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_starry_impact", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
        Count++;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hurt = new AnimState("Hurt");

        attack.NextState = idle;
        hurt.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Swing", attack);
        animator.AddAnyState("Hurt", hurt);

        return animator;
    }
}