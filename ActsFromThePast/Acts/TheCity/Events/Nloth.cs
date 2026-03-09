using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Nloth : EventModel
{
    private const string _choice1RelicKey = "Choice1Relic";
    private const string _choice2RelicKey = "Choice2Relic";

    private IReadOnlyList<RelicModel>? _choiceRelics;

    public override bool IsAllowed(RunState runState)
    {
        return runState.Players.All(p => GetValidRelics(p).Count() >= 2);
    }

    private IEnumerable<RelicModel> GetValidRelics(Player player)
    {
        return player.Relics.Where(r => r.IsTradable);
    }

    private IReadOnlyList<RelicModel> ChoiceRelics
    {
        get
        {
            AssertMutable();
            if (_choiceRelics == null)
            {
                _choiceRelics = GetValidRelics(Owner)
                    .ToList()
                    .StableShuffle(Rng)
                    .Take(2)
                    .ToList();
            }
            return _choiceRelics;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new StringVar(_choice1RelicKey),
                new StringVar(_choice2RelicKey)
            };
        }
    }

    public override void CalculateVars()
    {
        ((StringVar)DynamicVars[_choice1RelicKey]).StringValue = ChoiceRelics[0].Title.GetFormattedText();
        ((StringVar)DynamicVars[_choice2RelicKey]).StringValue = ChoiceRelics[1].Title.GetFormattedText();
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(TradeChoice1),
                "NLOTH.pages.INITIAL.options.TRADE_1",
                GetTradeHoverTips(0)),
            new EventOption(this, new Func<Task>(TradeChoice2),
                "NLOTH.pages.INITIAL.options.TRADE_2",
                GetTradeHoverTips(1)),
            new EventOption(this, new Func<Task>(LeaveOption),
                "NLOTH.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private IEnumerable<IHoverTip> GetTradeHoverTips(int index)
    {
        var giftRelic = ModelDb.Relic<NlothsGift>();
        return ChoiceRelics[index].HoverTips.Concat(giftRelic.HoverTips);
    }

    private async Task TradeChoice1() => await Trade(0);

    private async Task TradeChoice2() => await Trade(1);

    private async Task Trade(int index)
    {
        await RelicCmd.Remove(ChoiceRelics[index]);

        var gift = ModelDb.Relic<NlothsGift>().ToMutable();
        await RelicCmd.Obtain(gift, Owner);

        SetEventFinished(L10NLookup("NLOTH.pages.TRADE.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("NLOTH.pages.LEAVE.description"));
    }
}