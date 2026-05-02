using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CursedTome : CustomEventModel
{
    private const int DmgPage1 = 1;
    private const int DmgPage2 = 2;
    private const int DmgPage3 = 3;
    private const int DmgStop = 3;
    private const int DmgObtain = 15;
    private const int DmgRebalanced = 6;
    private const int DowngradeBase = 1;
    private const int UpgradeBase = 2;

    private int _downgrades;
    private int _upgrades;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("DmgPage1", DmgPage1),
        new IntVar("DmgPage2", DmgPage2),
        new IntVar("DmgPage3", DmgPage3),
        new IntVar("DmgStop", DmgStop),
        new IntVar("DmgObtain", DmgObtain),
        new IntVar("DmgRebalanced", DmgRebalanced),
        new IntVar("Downgrades", DowngradeBase),
        new IntVar("Upgrades", UpgradeBase)
    };

    public override void CalculateVars()
    {
        _downgrades = DowngradeBase;
        _upgrades = UpgradeBase;
        DynamicVars["Downgrades"].BaseValue = _downgrades;
        DynamicVars["Upgrades"].BaseValue = _upgrades;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Read, "INITIAL_REBALANCED"),
                PullAwayOption()
            };
        }

        return new[]
        {
            Option(Read),
            Option(Leave)
        };
    }

    private void AdvancePullAway()
    {
        _downgrades += DowngradeBase;
        _upgrades += UpgradeBase;
        DynamicVars["Downgrades"].BaseValue = _downgrades;
        DynamicVars["Upgrades"].BaseValue = _upgrades;
    }

    private EventOption PullAwayOption()
    {
        return new EventOption(this, PullAway,
            $"{Id.Entry}.pages.ALL.options.PULL_AWAY",
            Array.Empty<IHoverTip>()).ThatHasDynamicTitle();
    }

    private async Task PullAway()
    {
        var upgradedCards = Owner.Deck.Cards.Where(c => c.IsUpgraded).ToList();
        for (int i = 0; i < _downgrades && upgradedCards.Count > 0; i++)
        {
            var card = Rng.NextItem(upgradedCards);
            upgradedCards.Remove(card);
            CardCmd.Downgrade(card);
            CardCmd.Preview(card, style: CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        var upgradableCards = Owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        for (int i = 0; i < _upgrades && upgradableCards.Count > 0; i++)
        {
            var card = Rng.NextItem(upgradableCards);
            upgradableCards.Remove(card);
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        await Cmd.CustomScaledWait(0.6f, 1.2f);
        SetEventFinished(PageDescription("PULL_AWAY"));
    }

    private async Task Read()
    {
        ModAudio.Play("events", "cursed_tome");
        if (ActsFromThePastConfig.RebalancedMode)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DmgRebalanced,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_1"), new[]
            {
                new EventOption(this, Page1Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED",
                    Array.Empty<IHoverTip>()),
                PullAwayOption()
            });
        }
        else
        {
            SetEventState(PageDescription("PAGE_1"), new[]
            {
                new EventOption(this, Page1Continue,
                    $"{Id.Entry}.pages.PAGE_1.options.CONTINUE",
                    Array.Empty<IHoverTip>())
            });
        }
    }

    private async Task Page1Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage1,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_2"), new[]
            {
                new EventOption(this, Page2Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED",
                    Array.Empty<IHoverTip>()),
                PullAwayOption()
            });
        }
        else
        {
            SetEventState(PageDescription("PAGE_2"), new[]
            {
                new EventOption(this, Page2Continue,
                    $"{Id.Entry}.pages.PAGE_2.options.CONTINUE",
                    Array.Empty<IHoverTip>())
            });
        }
    }

    private async Task Page2Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage2,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_3"), new[]
            {
                new EventOption(this, Page3Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED",
                    Array.Empty<IHoverTip>()),
                PullAwayOption()
            });
        }
        else
        {
            SetEventState(PageDescription("PAGE_3"), new[]
            {
                new EventOption(this, Page3Continue,
                    $"{Id.Entry}.pages.PAGE_3.options.CONTINUE",
                    Array.Empty<IHoverTip>())
            });
        }
    }

    private async Task Page3Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage3,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("LAST_PAGE"), new[]
            {
                Option(Obtain, "LAST_PAGE_REBALANCED"),
                PullAwayOption()
            });
        }
        else
        {
            SetEventState(PageDescription("LAST_PAGE"), new[]
            {
                Option(Obtain, "LAST_PAGE"),
                Option(Stop, "LAST_PAGE")
            });
        }
    }

    private async Task Obtain()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgObtain,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        var relic = GetRandomBook().ToMutable();
        await RewardsCmd.OfferCustom(Owner, new List<Reward>(1)
        {
            new RelicReward(relic, Owner)
        });
        SetEventFinished(PageDescription("OBTAIN"));
    }

    private async Task Stop()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgStop,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        SetEventFinished(PageDescription("STOP"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }

    private RelicModel GetRandomBook()
    {
        var possibleBooks = new List<RelicModel>();
        if (!Owner.Relics.Any(r => r is Necronomicon))
            possibleBooks.Add(ModelDb.Relic<Necronomicon>());
        if (!Owner.Relics.Any(r => r is Enchiridion))
            possibleBooks.Add(ModelDb.Relic<Enchiridion>());
        if (!Owner.Relics.Any(r => r is NilrysCodex))
            possibleBooks.Add(ModelDb.Relic<NilrysCodex>());
        if (possibleBooks.Count == 0)
            return RelicFactory.PullNextRelicFromFront(Owner);
        return Rng.NextItem(possibleBooks);
    }
}