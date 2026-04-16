using ActsFromThePast.Cards;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
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

public sealed class Augmenter : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Deck.Cards.Count(c => c.IsRemovable) >= 2);
    }

    private bool CanTransform()
    {
        return PileType.Deck.GetPile(Owner).Cards.Count(c => c.IsRemovable) >= 2;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            // Option 1: Obtain J.A.X.
            Option(Jax, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Jax>()))
        };

        // Option 2: Transform 2 cards
        if (CanTransform())
            options.Add(Option(Transform));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.TRANSFORM_LOCKED",
                Array.Empty<IHoverTip>()));

        // Option 3: Obtain Mutagenic Strength
        options.Add(Option(Mutagens, "INITIAL",
            HoverTipFactory.FromRelic(ModelDb.Relic<MutagenicStrength>()).ToArray()));

        return options;
    }

    private async Task Jax()
    {
        var jax = Owner.RunState.CreateCard(ModelDb.Card<Jax>(), Owner);
        var result = await CardPileCmd.Add(jax, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
        SetEventFinished(PageDescription("JAX"));
    }

    private async Task Transform()
    {
        var prefs = new CardSelectorPrefs(
            L10NLookup($"{Id.Entry}.pages.TRANSFORM.selectionScreenPrompt"), 2);
        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);
        foreach (var card in selectedCards.ToList())
        {
            await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche, CardPreviewStyle.HorizontalLayout);
        }
        SetEventFinished(PageDescription("TRANSFORM"));
    }

    private async Task Mutagens()
    {
        await RelicCmd.Obtain(ModelDb.Relic<MutagenicStrength>().ToMutable(), Owner);
        SetEventFinished(PageDescription("MUTAGENS"));
    }
}