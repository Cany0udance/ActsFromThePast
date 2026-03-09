using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Acts.TheCity.Events;
public sealed class AncientWriting : EventModel
{
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ancient_writing");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(EleganceOption),
                "ANCIENT_WRITING.pages.INITIAL.options.ELEGANCE",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(SimplicityOption),
                "ANCIENT_WRITING.pages.INITIAL.options.SIMPLICITY",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task EleganceOption()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());
        SetEventFinished(L10NLookup("ANCIENT_WRITING.pages.ELEGANCE.description"));
    }

    private async Task SimplicityOption()
    {
        var cardsToUpgrade = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.Rarity == CardRarity.Basic 
                        && (c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend))
                        && c.IsUpgradable)
            .ToList();

        CardCmd.Upgrade(cardsToUpgrade, CardPreviewStyle.EventLayout);
        
        SetEventFinished(L10NLookup("ANCIENT_WRITING.pages.SIMPLICITY.description"));
    }
}