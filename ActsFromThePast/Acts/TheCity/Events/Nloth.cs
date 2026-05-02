using System.Reflection;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Nloth : CustomEventModel
{
    private const string _choice1RelicKey = "Choice1Relic";
    private const string _choice2RelicKey = "Choice2Relic";
    private IReadOnlyList<RelicModel>? _choiceRelics;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    public override bool IsAllowed(IRunState runState)
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

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new StringVar(_choice1RelicKey),
        new StringVar(_choice2RelicKey)
    };

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
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                new EventOption(this, TradeChoice1,
                    $"{Id.Entry}.pages.INITIAL.options.TRADE_1",
                    GetTradeHoverTips(0).ToArray()),
                new EventOption(this, TradeChoice2,
                    $"{Id.Entry}.pages.INITIAL.options.TRADE_2",
                    GetTradeHoverTips(1).ToArray()),
                Option(SearchWithNloth, "INITIAL_REBALANCED")
            };
        }

        return new[]
        {
            new EventOption(this, TradeChoice1,
                $"{Id.Entry}.pages.INITIAL.options.TRADE_1",
                GetTradeHoverTips(0).ToArray()),
            new EventOption(this, TradeChoice2,
                $"{Id.Entry}.pages.INITIAL.options.TRADE_2",
                GetTradeHoverTips(1).ToArray()),
            Option(Leave)
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
        SetEventFinished(PageDescription("TRADE"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
    
    private async Task SearchWithNloth()
    {
        var trashHeapCards = typeof(TrashHeap)
            .GetProperty("Cards", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null) as CardModel[];

        var options = CardCreationOptions
            .ForNonCombatWithUniformOdds(
                new[] { Owner.Character.CardPool },
                c => c.Rarity == CardRarity.Common)
            .WithFlags(CardCreationFlags.NoRarityModification);

        var results = CardFactory.CreateForReward(Owner, 5, options).ToList();

        if (trashHeapCards != null)
        {
            var shuffled = trashHeapCards.ToList().StableShuffle(Rng);
            int trashIndex = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (Rng.NextInt(100) < 15 && trashIndex < shuffled.Count)
                {
                    var created = Owner.RunState.CreateCard(shuffled[trashIndex], Owner);
                    results[i] = new CardCreationResult(created);
                    trashIndex++;
                }
            }
        }

        var prefs = new CardSelectorPrefs(
            L10NLookup($"{Id.Entry}.pages.SEARCH_WITH_NLOTH.selectionScreenPrompt"), 1);
        foreach (var card in await CardSelectCmd.FromSimpleGridForRewards(
                     new BlockingPlayerChoiceContext(), results, Owner, prefs))
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        }

        SetEventFinished(PageDescription("SEARCH_WITH_NLOTH"));
    }
}