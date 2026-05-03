

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class UpgradeShrine : CustomEventModel
{
    public override ActModel[] Acts => Array.Empty<ActModel>();

    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c => c.IsUpgradable));
    }
    
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Deck.Cards.Any(c => c.IsUpgradable))
        {
            options.Add(Option(Pray));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.PRAY_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        if (ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Kneel, "INITIAL_REBALANCED"));
        }
        else
        {
            options.Add(Option(Leave));
        }

        return options;
    }

    private async Task Pray()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();

        if (card != null)
            CardCmd.Upgrade(card);

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private Task Kneel()
    {
        foreach (var card in Owner.Deck.Cards
                     .Where(c => c.IsUpgradable)
                     .ToList()
                     .StableShuffle(Owner.RunState.Rng.Niche)
                     .Take(2))
        {
            CardCmd.Upgrade(card);
        }

        SetEventFinished(PageDescription("PRAY"));
        return Task.CompletedTask;
    }
}