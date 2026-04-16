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

public sealed class ShiftingPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private int _pendingStrengthRestore = 0;

    private int PendingStrengthRestore
    {
        get => _pendingStrengthRestore;
        set { AssertMutable(); _pendingStrengthRestore = value; }
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
        if (result.TotalDamage <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, -result.TotalDamage, Owner, null);
        PendingStrengthRestore += result.TotalDamage;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        if (PendingStrengthRestore <= 0)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, PendingStrengthRestore, Owner, null);
        PendingStrengthRestore = 0;
    }
}