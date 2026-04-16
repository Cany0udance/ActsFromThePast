using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Sssserpent : CustomEventModel
{
    private const int GoldReward = 150;

    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(GoldReward)
    };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Agree, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
            Option(Disagree)
        };
    }

    private Task Agree()
    {
        SetEventState(PageDescription("AGREE"), new[]
        {
            Option(TakeGold, "AGREE")
        });
        return Task.CompletedTask;
    }

    private async Task TakeGold()
    {
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("TAKE_GOLD"));
    }

    private Task Disagree()
    {
        SetEventFinished(PageDescription("DISAGREE"));
        return Task.CompletedTask;
    }
}