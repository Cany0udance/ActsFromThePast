using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.SharedEvents;

public sealed class Transmogrifier : CustomEventModel
{
    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Pray),
                Option(Kneel, "INITIAL_REBALANCED")
            };
        }
        return new[]
        {
            Option(Pray),
            Option(Leave)
        };
    }
    private async Task Pray()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
        {
            await CardCmd.TransformToRandom(original, Rng, CardPreviewStyle.EventLayout);
        }

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private async Task Kneel()
    {
        var cards = Owner.Deck.Cards
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2)
            .ToList();

        foreach (var original in cards)
        {
            var transformed = CardFactory.CreateRandomCardForTransform(original, false, Owner.RunState.Rng.Niche);
            CardCmd.Upgrade(transformed);
            await CardCmd.Transform(original, transformed);
        }

        SetEventFinished(PageDescription("PRAY"));
    }
}