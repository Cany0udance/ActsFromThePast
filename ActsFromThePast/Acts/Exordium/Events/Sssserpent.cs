using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Sssserpent : EventModel
{
    private const int GoldReward = 150;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new GoldVar(GoldReward)
            };
        }
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(AgreeOption),
                "SSSSERPENT.pages.INITIAL.options.AGREE",
                HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
            new EventOption(this, new Func<Task>(DisagreeOption),
                "SSSSERPENT.pages.INITIAL.options.DISAGREE",
                Array.Empty<IHoverTip>())
        };
    }

    private Task AgreeOption()
    {
        SetEventState(L10NLookup("SSSSERPENT.pages.AGREE.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(TakeGoldOption),
                "SSSSERPENT.pages.AGREE.options.TAKE_GOLD",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }

    private async Task TakeGoldOption()
    {
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(L10NLookup("SSSSERPENT.pages.TAKE_GOLD.description"));
    }

    private Task DisagreeOption()
    {
        SetEventFinished(L10NLookup("SSSSERPENT.pages.DISAGREE.description"));
        return Task.CompletedTask;
    }
}