using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class FlightPower : PowerModel
{
    
    // TODO figure out how to implement "on apply" sound
    
    private const string _storedAmountKey = "StoredAmount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new DynamicVar(_storedAmountKey, 0M)
            };
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[_storedAmountKey].BaseValue = Amount;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (side != Owner.Side)
            return;

        int stored = (int)DynamicVars[_storedAmountKey].BaseValue;
        if (Amount != stored)
            await PowerCmd.SetAmount<FlightPower>(Owner, stored, null, null);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return 1M;
        return 0.5M;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return;
        if (target.CurrentHp <= 0)
            return;

        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);

        if (oldOwner.Monster is Byrd byrd)
        {
            await byrd.OnFlightBroken();
        }
    }
}