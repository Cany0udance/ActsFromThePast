using ActsFromThePast.Enchantments;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class TombOfLordRedMask : CustomEventModel
{
    private const int GoldAmount = 222;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };
    
    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        var fearful = ModelDb.Enchantment<Fearful>();
        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c =>
                c.Type == CardType.Skill && fearful.CanEnchant(c)));
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(GoldAmount),
        new IntVar("PlayerGold", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["PlayerGold"].BaseValue = Owner.Gold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is RedMask))
        {
            options.Add(Option(WearMask));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.WEAR_MASK_LOCKED",
                Array.Empty<IHoverTip>()));
            options.Add(Option(PayRespects, "INITIAL",
                HoverTipFactory.FromRelic(ModelDb.Relic<RedMask>()).ToArray()));
        }

        if (ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(new EventOption(this, Run,
                L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.RUN.title"),
                L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.RUN.description"),
                $"{Id.Entry}.pages.INITIAL_REBALANCED.options.RUN",
                HoverTipFactory.FromEnchantment<Fearful>()));
        }
        else
        {
            options.Add(Option(Leave));
        }

        return options;
    }

    private async Task WearMask()
    {
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(PageDescription("WEAR_MASK"));
    }

    private async Task PayRespects()
    {
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
        {
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Spent);
        }
        var redMask = ModelDb.Relic<RedMask>().ToMutable();
        await RelicCmd.Obtain(redMask, Owner);
        SetEventFinished(PageDescription("PAY_RESPECTS"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private async Task Run()
    {
        var fearfulModel = ModelDb.Enchantment<Fearful>();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForEnchantment(
            Owner, fearfulModel, 0, c => c.Type == CardType.Skill, prefs)).FirstOrDefault();

        if (card != null)
        {
            CardCmd.Enchant<Fearful>(card, 0M);
            var child = NCardEnchantVfx.Create(card);
            if (child != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(child);
        }

        SetEventFinished(PageDescription("RUN"));
    }
}