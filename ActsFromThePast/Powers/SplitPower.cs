using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class SplitPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldStopCombatFromEnding() => true;

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
        if (target.CurrentHp > target.MaxHp / 2)
            return;

        if (Owner.Monster is AcidSlimeLarge acidSlime)
        {
            if (acidSlime.SplitTriggered)
                return;
            Flash();
            acidSlime.SplitTriggered = true;
            acidSlime.SetMoveImmediate(acidSlime.SplitState, true);
        }
        else if (Owner.Monster is SpikeSlimeLarge spikeSlime)
        {
            if (spikeSlime.SplitTriggered)
                return;
            Flash();
            spikeSlime.SplitTriggered = true;
            spikeSlime.SetMoveImmediate(spikeSlime.SplitState, true);
        }
        else if (Owner.Monster is SlimeBoss slimeBoss)
        {
            if (slimeBoss.SplitTriggered)
                return;
            Flash();
            slimeBoss.SplitTriggered = true;
            slimeBoss.SetMoveImmediate(slimeBoss.SplitState, true);
        }
    }
}