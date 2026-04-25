using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public class StrengthUpPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        StrengthUpPower strengthUpPower = this;
        if (side != strengthUpPower.Owner.Side)
            return;
        strengthUpPower.Flash();
        StrengthPower strengthPower = await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), strengthUpPower.Owner, (Decimal) strengthUpPower.Amount, strengthUpPower.Owner, (CardModel) null);
    }
}