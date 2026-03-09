using ActsFromThePast.Cards;
using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Augmenter : EventModel
{
    public override bool IsAllowed(RunState runState)
    {
        return runState.Players.All(p => p.Deck.Cards.Count(c => c.IsRemovable) >= 2);
    }

    private bool CanTransform()
    {
        return PileType.Deck.GetPile(Owner).Cards.Count(c => c.IsRemovable) >= 2;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        // Option 1: Obtain J.A.X.
        options.Add(new EventOption(this, new Func<Task>(JaxOption),
            "AUGMENTER.pages.INITIAL.options.JAX",
            HoverTipFactory.FromCard(ModelDb.Card<Jax>())));

        // Option 2: Transform 2 cards
        if (CanTransform())
        {
            options.Add(new EventOption(this, new Func<Task>(TransformOption),
                "AUGMENTER.pages.INITIAL.options.TRANSFORM",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "AUGMENTER.pages.INITIAL.options.TRANSFORM_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Option 3: Obtain Mutagenic Strength
        options.Add(new EventOption(this, new Func<Task>(MutagensOption),
            "AUGMENTER.pages.INITIAL.options.MUTAGENS",
            HoverTipFactory.FromRelic(ModelDb.Relic<MutagenicStrength>())));

        return options;
    }

    private async Task JaxOption()
    {
        // TODO: Implement J.A.X. card
        var jax = Owner.RunState.CreateCard(ModelDb.Card<Jax>(), Owner);
        var result = await CardPileCmd.Add(jax, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);

        SetEventFinished(L10NLookup("AUGMENTER.pages.JAX.description"));
    }

    private async Task TransformOption()
    {
        var prefs = new CardSelectorPrefs(L10NLookup("AUGMENTER.pages.TRANSFORM.selectionScreenPrompt"), 2);
        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);

        foreach (var card in selectedCards.ToList())
        {
            await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche, CardPreviewStyle.HorizontalLayout);
        }

        SetEventFinished(L10NLookup("AUGMENTER.pages.TRANSFORM.description"));
    }

    private async Task MutagensOption()
    {
        await RelicCmd.Obtain(ModelDb.Relic<MutagenicStrength>().ToMutable(), Owner);
        SetEventFinished(L10NLookup("AUGMENTER.pages.MUTAGENS.description"));
    }
}