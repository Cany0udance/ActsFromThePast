using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class TombOfLordRedMask : EventModel
{
    private const int GoldAmount = 222;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new GoldVar(GoldAmount),
                new IntVar("PlayerGold", 0)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars["PlayerGold"].BaseValue = Owner.Gold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is RedMask))
        {
            options.Add(new EventOption(this, WearMaskOption,
                "TOMB_OF_LORD_RED_MASK.pages.INITIAL.options.WEAR_MASK"));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "TOMB_OF_LORD_RED_MASK.pages.INITIAL.options.WEAR_MASK_LOCKED"));
            options.Add(new EventOption(this, PayRespectsOption,
                "TOMB_OF_LORD_RED_MASK.pages.INITIAL.options.PAY_RESPECTS",
                HoverTipFactory.FromRelic(ModelDb.Relic<RedMask>())));
        }

        options.Add(new EventOption(this, LeaveOption,
            "TOMB_OF_LORD_RED_MASK.pages.INITIAL.options.LEAVE"));

        return options;
    }

    private async Task WearMaskOption()
    {
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(L10NLookup("TOMB_OF_LORD_RED_MASK.pages.WEAR_MASK.description"));
    }

    private async Task PayRespectsOption()
    {
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
        {
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Spent);
        }
        var redMask = ModelDb.Relic<RedMask>().ToMutable();
        await RelicCmd.Obtain(redMask, Owner);
        SetEventFinished(L10NLookup("TOMB_OF_LORD_RED_MASK.pages.PAY_RESPECTS.description"));
    }

    private Task LeaveOption()
    {
        SetEventFinished(L10NLookup("TOMB_OF_LORD_RED_MASK.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}