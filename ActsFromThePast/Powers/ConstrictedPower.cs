using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ConstrictedPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        ConstrictedPower constrictedPower = this;
        if (side != constrictedPower.Owner.Side)
            return;
        IEnumerable<DamageResult> damageResults = await CreatureCmd.Damage(choiceContext, constrictedPower.Owner, (Decimal) constrictedPower.Amount, ValueProp.Unpowered, constrictedPower.Owner, (CardModel) null);
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        ConstrictedPower power = this;
        if (wasRemovalPrevented || creature != power.Applier)
            return;
        await PowerCmd.Remove((PowerModel) power);
    }
}
