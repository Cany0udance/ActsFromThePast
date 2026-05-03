using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.SharedEvents;

public sealed class GoldenShrine : CustomEventModel
{
    
    // TODO add gold VFX to this
    
    private const int GoldAmount = 50;
    private const int CurseGoldAmount = 275;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(GoldAmount),
        new IntVar("CurseGold", CurseGoldAmount)
    };
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Pray),
            Option(Desecrate, "INITIAL",
                HoverTipFactory.FromCardWithCardHoverTips<Regret>().ToArray())
        };

        if (!ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Leave));
        }

        return options;
    }

    private async Task Pray()
    {
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task Desecrate()
    {
        await PlayerCmd.GainGold(CurseGoldAmount, Owner);
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        SetEventFinished(PageDescription("DESECRATE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}