using ActsFromThePast.Cards;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class OminousForge : CustomEventModel
{
    public override ActModel[] Acts => Array.Empty<ActModel>();
    
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c => c.IsUpgradable));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();
        
        options.Add(Option(Forge));

        options.Add(Option(Rummage, "INITIAL",
            HoverTipFactory.FromCardWithCardHoverTips<Pain>()
                .Concat(HoverTipFactory.FromRelic<WarpedTongs>())
                .ToArray()));

        if (!ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Leave));
        }

        return options;
    }
    
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ominous_forge");
    }

    private async Task Forge()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();
        if (card != null)
            CardCmd.Upgrade(card);

        SetEventFinished(PageDescription("FORGE"));
    }

    private async Task Rummage()
    {
        await CardPileCmd.AddCurseToDeck<Pain>(Owner);
        await RelicCmd.Obtain(ModelDb.Relic<WarpedTongs>().ToMutable(), Owner);

        SetEventFinished(PageDescription("RUMMAGE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}