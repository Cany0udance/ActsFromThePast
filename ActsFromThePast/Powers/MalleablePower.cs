using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class MalleablePower : PowerModel
{
    private const string _baseAmountKey = "BaseAmount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new DynamicVar(_baseAmountKey, 3M)
            };
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[_baseAmountKey].BaseValue = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (props.HasFlag(ValueProp.Unpowered))
            return;
        if (!props.HasFlag(ValueProp.Move))
            return;
        if (target.CurrentHp <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, (decimal)Amount, ValueProp.Unpowered, null);
        await PowerCmd.ModifyAmount(this, 1, null, null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        int baseAmount = (int)DynamicVars[_baseAmountKey].BaseValue;
        if (Amount != baseAmount)
            await PowerCmd.SetAmount<MalleablePower>(Owner, baseAmount, null, null);
    }
}