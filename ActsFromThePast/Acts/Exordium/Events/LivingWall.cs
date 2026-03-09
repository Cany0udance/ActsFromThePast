using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class LivingWall : EventModel
{
    public override bool IsAllowed(RunState runState)
    {
        return runState.Players.All(p => p.Deck.Cards.Any(c => c.IsRemovable));
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "living_wall");
    }

    private bool HasUpgradableCards()
    {
        return PileType.Deck.GetPile(Owner).Cards.Any(c => c != null && c.IsUpgradable);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            new EventOption(this, new Func<Task>(ForgetOption),
                "LIVING_WALL.pages.INITIAL.options.FORGET",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(ChangeOption),
                "LIVING_WALL.pages.INITIAL.options.CHANGE",
                Array.Empty<IHoverTip>())
        };

        if (HasUpgradableCards())
        {
            options.Add(new EventOption(this, new Func<Task>(GrowOption),
                "LIVING_WALL.pages.INITIAL.options.GROW",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "LIVING_WALL.pages.INITIAL.options.GROW_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        return options;
    }

    private async Task ForgetOption()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());
        SetEventFinished(L10NLookup("LIVING_WALL.pages.RESULT.description"));
    }

    private async Task ChangeOption()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);
        foreach (var card in selectedCards.ToList())
        {
            await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche, CardPreviewStyle.HorizontalLayout);
        }
        SetEventFinished(L10NLookup("LIVING_WALL.pages.RESULT.description"));
    }

    private async Task GrowOption()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        foreach (var card in selectedCards)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(L10NLookup("LIVING_WALL.pages.RESULT.description"));
    }
}