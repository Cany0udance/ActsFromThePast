using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class PleadingVagrant : EventModel
{
    private const int GoldCost = 85;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("GoldCost", GoldCost)
            };
        }
    }

    private bool CanAfford()
    {
        return Owner.Gold >= GoldCost;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (CanAfford())
        {
            options.Add(new EventOption(this, new Func<Task>(PayGoldOption),
                "PLEADING_VAGRANT.pages.INITIAL.options.PAY_GOLD",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "PLEADING_VAGRANT.pages.INITIAL.options.PAY_GOLD_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        options.Add(new EventOption(this, new Func<Task>(RobOption),
            "PLEADING_VAGRANT.pages.INITIAL.options.ROB",
            HoverTipFactory.FromCard(ModelDb.Card<Shame>())));

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "PLEADING_VAGRANT.pages.INITIAL.options.LEAVE",
            Array.Empty<IHoverTip>()));

        return options;
    }

    private async Task PayGoldOption()
    {
        await PlayerCmd.LoseGold(GoldCost, Owner);

        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        SetEventFinished(L10NLookup("PLEADING_VAGRANT.pages.PAY_GOLD.description"));
    }

    private async Task RobOption()
    {
        var shame = Owner.RunState.CreateCard(ModelDb.Card<Shame>(), Owner);
        var curseResult = await CardPileCmd.Add(shame, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);

        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        SetEventFinished(L10NLookup("PLEADING_VAGRANT.pages.ROB.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("PLEADING_VAGRANT.pages.LEAVE.description"));
    }
}