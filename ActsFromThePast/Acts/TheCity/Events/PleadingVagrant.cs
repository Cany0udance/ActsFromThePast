using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class PleadingVagrant : CustomEventModel
{
    private const int GoldCost = 85;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GoldCost", GoldCost)
    };

    private bool CanAfford()
    {
        return Owner.Gold >= GoldCost;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (CanAfford())
            options.Add(Option(PayGold));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.PAY_GOLD_LOCKED",
                Array.Empty<IHoverTip>()));

        options.Add(Option(Rob, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Shame>())));
        options.Add(Option(Leave));
        return options;
    }

    private async Task PayGold()
    {
        await PlayerCmd.LoseGold(GoldCost, Owner);
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("PAY_GOLD"));
    }

    private async Task Rob()
    {
        var shame = Owner.RunState.CreateCard(ModelDb.Card<Shame>(), Owner);
        var curseResult = await CardPileCmd.Add(shame, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("ROB"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}