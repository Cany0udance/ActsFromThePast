using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class AsleepLagavulinPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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
        if (result.UnblockedDamage == 0)
            return;

        var lagavulin = (Lagavulin)Owner.Monster;

        if (Owner.HasPower<MetallicizePower>())
            await PowerCmd.Remove(Owner.GetPower<MetallicizePower>());

        await lagavulin.WakeUpFromDamage();

        await PowerCmd.Remove(this);
    }
    
    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (side != CombatSide.Player || combatState.RoundNumber != 1)
            return Task.CompletedTask;
    
        return CreatureCmd.GainBlock(Owner, 8, ValueProp.Unpowered, null);
    }

    public override async Task BeforeTurnEndVeryEarly(
        PlayerChoiceContext choiceContext,
        CombatSide side)
    {
        if (side != Owner.Side || Amount > 1 || !Owner.HasPower<MetallicizePower>())
            return;

        await PowerCmd.Remove(Owner.GetPower<MetallicizePower>());
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Decrement(this);

        if (Amount > 0)
            return;

        var lagavulin = (Lagavulin)Owner.Monster;
        await lagavulin.WakeUpNaturally();
    }
}