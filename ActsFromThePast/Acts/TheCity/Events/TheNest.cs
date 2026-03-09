using ActsFromThePast.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheNest : EventModel
{
    private const int HpLoss = 6;
    private const int GoldGain = 50;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("HpLoss", HpLoss),
                new GoldVar(GoldGain)
            };
        }
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(InvestigateOption),
                "THE_NEST.pages.INITIAL.options.INVESTIGATE",
                Array.Empty<IHoverTip>())
        };
    }

    private Task InvestigateOption()
    {
        SetEventState(L10NLookup("THE_NEST.pages.INVESTIGATE.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(StealOption),
                "THE_NEST.pages.INVESTIGATE.options.STEAL",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(JoinOption),
                "THE_NEST.pages.INVESTIGATE.options.JOIN",
                HoverTipFactory.FromCard(ModelDb.Card<RitualDagger>()))
                .ThatDoesDamage(HpLoss)
        });
        return Task.CompletedTask;
    }

    private async Task StealOption()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(L10NLookup("THE_NEST.pages.STEAL.description"));
    }

    private async Task JoinOption()
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

        SetEventFinished(L10NLookup("THE_NEST.pages.JOIN.description"));
    }
}