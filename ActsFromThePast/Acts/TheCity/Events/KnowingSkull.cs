using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class KnowingSkull : CustomEventModel
{
    private const int BaseCost = 6;
    private const int GoldReward = 90;
    private int _potionCost = BaseCost;
    private int _cardCost = BaseCost;
    private int _goldCost = BaseCost;
    private int _leaveCost = BaseCost;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("PotionCost", BaseCost),
        new IntVar("CardCost", BaseCost),
        new IntVar("GoldCost", BaseCost),
        new IntVar("LeaveCost", BaseCost),
        new IntVar("GoldReward", GoldReward)
    };

    private void UpdateDynamicVars()
    {
        DynamicVars["PotionCost"].BaseValue = _potionCost;
        DynamicVars["CardCost"].BaseValue = _cardCost;
        DynamicVars["GoldCost"].BaseValue = _goldCost;
        DynamicVars["LeaveCost"].BaseValue = _leaveCost;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "knowing_skull");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        SetAskState(PageDescription("ASK"));
        return Task.CompletedTask;
    }

    private void SetAskState(LocString description)
    {
        UpdateDynamicVars();
        SetEventState(description, new[]
        {
            Option(Potion, "ASK"),
            Option(Gold, "ASK"),
            Option(Card, "ASK"),
            Option(Leave, "ASK")
        });
    }

    private async Task Potion()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _potionCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        _potionCost++;

        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.Niche).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
        SetAskState(PageDescription("POTION"));
    }

    private async Task Gold()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _goldCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        _goldCost++;
        await PlayerCmd.GainGold(GoldReward, Owner);
        SetAskState(PageDescription("GOLD"));
    }

    private async Task Card()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _cardCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        _cardCost++;

        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Uncommon)
            .ToList();

        var chosenCard = Owner.RunState.Rng.Niche.NextItem(colorlessCards);
        var card = Owner.RunState.CreateCard(chosenCard, Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
        SetAskState(PageDescription("CARD"));
    }

    private async Task Leave()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _leaveCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(PageDescription("LEAVE"));
    }
}