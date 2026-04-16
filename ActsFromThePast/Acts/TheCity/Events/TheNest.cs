using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheNest : CustomEventModel
{
    private const int HpLoss = 6;
    private const int GoldGain = 50;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", HpLoss),
        new GoldVar(GoldGain)
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Investigate) };
    }

    private Task Investigate()
    {
        SetEventState(PageDescription("INVESTIGATE"), new[]
        {
            Option(Steal, "INVESTIGATE"),
            Option(Join, "INVESTIGATE", HoverTipFactory.FromCard(ModelDb.Card<RitualDagger>()))
                .ThatDoesDamage(HpLoss)
        });
        return Task.CompletedTask;
    }

    private async Task Steal()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("STEAL"));
    }

    private async Task Join()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            HpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        var ritualDagger = Owner.RunState.CreateCard(ModelDb.Card<RitualDagger>(), Owner);
        var addResult = await CardPileCmd.Add(ritualDagger, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("JOIN"));
    }
}