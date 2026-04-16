using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Acts.TheCity.Events;
public sealed class AncientWriting : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ancient_writing");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Elegance),
            Option(Simplicity)
        };
    }

    private async Task Elegance()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());
        SetEventFinished(PageDescription("ELEGANCE"));
    }

    private async Task Simplicity()
    {
        var cardsToUpgrade = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.Rarity == CardRarity.Basic
                        && (c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend))
                        && c.IsUpgradable)
            .ToList();
        CardCmd.Upgrade(cardsToUpgrade, CardPreviewStyle.EventLayout);

        SetEventFinished(PageDescription("SIMPLICITY"));
    }
}