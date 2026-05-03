using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class WeMeetAgain : CustomEventModel
{
    private const int MinGold = 50;
    private const int MaxGold = 150;

    private PotionModel _potionOption;
    private CardModel _cardOption;
    private int _goldAmount;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GoldAmount", 0),
        new StringVar("PotionName", ""),
        new StringVar("CardName", "")
    };
    
    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
        {
            int validOptions = 0;

            if (p.Potions.Any())
                validOptions++;

            if (p.Gold >= MinGold)
                validOptions++;

            if (PileType.Deck.GetPile(p).Cards.Any(c =>
                    c.Rarity != CardRarity.Basic && c.Type != CardType.Curse))
                validOptions++;

            return validOptions >= 2;
        });
    }

    public override void CalculateVars()
    {
        _potionOption = Owner.Potions.Any()
            ? Rng.NextItem(Owner.Potions)
            : null;

        var nonBasicCards = Owner.Deck.Cards
            .Where(c => c.Rarity != CardRarity.Basic && c.Type != CardType.Curse)
            .ToList();
        _cardOption = nonBasicCards.Any()
            ? Rng.NextItem(nonBasicCards)
            : null;

        if (Owner.Gold < MinGold)
            _goldAmount = 0;
        else if (Owner.Gold > MaxGold)
            _goldAmount = Rng.NextInt(MinGold, MaxGold + 1);
        else
            _goldAmount = Rng.NextInt(MinGold, Owner.Gold + 1);

        DynamicVars["GoldAmount"].BaseValue = _goldAmount;

        if (_potionOption != null)
            ((StringVar)DynamicVars["PotionName"]).StringValue = _potionOption.Title.GetFormattedText();
        if (_cardOption != null)
            ((StringVar)DynamicVars["CardName"]).StringValue = _cardOption.Title;
    }

    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        Owner.CanRemovePotions = false;
        return Task.CompletedTask;
    }

    protected override void OnEventFinished()
    {
        Owner.CanRemovePotions = true;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        // Give Potion
        if (_potionOption != null)
        {
            var title = L10NLookup($"{Id.Entry}.pages.INITIAL.options.GIVE_POTION.title");
            var desc = L10NLookup($"{Id.Entry}.pages.INITIAL.options.GIVE_POTION.description");
            options.Add(new EventOption(this,
                async () => await GivePotion(_potionOption),
                title, desc,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_POTION",
                _potionOption.HoverTips).ThatHasDynamicTitle());
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_POTION_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Give Gold
        if (_goldAmount > 0)
        {
            options.Add(Option(GiveGold).ThatHasDynamicTitle());
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_GOLD_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Give Card
        if (_cardOption != null)
        {
            var title = L10NLookup($"{Id.Entry}.pages.INITIAL.options.GIVE_CARD.title");
            var desc = L10NLookup($"{Id.Entry}.pages.INITIAL.options.GIVE_CARD.description");
            options.Add(new EventOption(this,
                async () => await GiveCard(_cardOption),
                title, desc,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_CARD",
                new[] { HoverTipFactory.FromCard(_cardOption) }).ThatHasDynamicTitle());
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_CARD_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Attack
        if (!ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Attack));
        }

        return options;
    }

    private async Task GivePotion(PotionModel potion)
    {
        await PotionCmd.Discard(potion);
        await RelicCmd.Obtain(
            RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("GIVE_POTION"));
    }

    private async Task GiveGold()
    {
        await PlayerCmd.LoseGold(_goldAmount, Owner, GoldLossType.Spent);
        await RelicCmd.Obtain(
            RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("GIVE_GOLD"));
    }

    private async Task GiveCard(CardModel card)
    {
        await CardPileCmd.RemoveFromDeck(new List<CardModel> { card });
        await RelicCmd.Obtain(
            RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("GIVE_CARD"));
    }

    private Task Attack()
    {
        SetEventFinished(PageDescription("ATTACK"));
        return Task.CompletedTask;
    }
}