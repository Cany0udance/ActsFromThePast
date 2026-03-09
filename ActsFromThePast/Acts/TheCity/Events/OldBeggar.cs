using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class OldBeggar : EventModel
{
    private const int GoldCost = 75;

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
            options.Add(new EventOption(this, new Func<Task>(GiveGoldOption),
                "OLD_BEGGAR.pages.INITIAL.options.GIVE_GOLD",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "OLD_BEGGAR.pages.INITIAL.options.GIVE_GOLD_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "OLD_BEGGAR.pages.INITIAL.options.LEAVE",
            Array.Empty<IHoverTip>()));

        return options;
    }

    private async Task GiveGoldOption()
    {
        // TODO: Change event image to cleric
        // this.imageEventText.loadImage("images/events/cleric.jpg");

        await PlayerCmd.LoseGold(GoldCost, Owner);

        SetEventState(L10NLookup("OLD_BEGGAR.pages.GAVE_GOLD.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(RemoveCardOption),
                "OLD_BEGGAR.pages.GAVE_GOLD.options.REMOVE_CARD",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task RemoveCardOption()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());

        SetEventFinished(L10NLookup("OLD_BEGGAR.pages.REMOVE_CARD.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("OLD_BEGGAR.pages.LEAVE.description"));
    }
}