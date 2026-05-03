using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class Duplicator : CustomEventModel
{
    public override ActModel[] Acts => Array.Empty<ActModel>();
    
    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c => c.IsUpgraded));
    }

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
        var prefs = new CardSelectorPrefs(L10NLookup($"{Id.Entry}.SELECT_DUPLICATE"), 1);
        var card = (await CardSelectCmd.FromDeckGeneric(Owner, prefs)).FirstOrDefault();

        if (card != null)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(Owner.RunState.CloneCard(card), PileType.Deck));

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private async Task Kneel()
    {
        var upgraded = Owner.Deck.Cards
            .Where(c => c.IsUpgraded)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2)
            .ToList();

        foreach (var card in upgraded)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.Add(Owner.RunState.CloneCard(card), PileType.Deck));
        }

        SetEventFinished(PageDescription("PRAY"));
    }
}