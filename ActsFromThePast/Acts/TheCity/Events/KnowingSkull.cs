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

public sealed class KnowingSkull : EventModel
{
    private const int BaseCost = 6;
    private const int GoldReward = 90;

    private int _potionCost = BaseCost;
    private int _cardCost = BaseCost;
    private int _goldCost = BaseCost;
    private int _leaveCost = BaseCost;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("PotionCost", BaseCost),
                new IntVar("CardCost", BaseCost),
                new IntVar("GoldCost", BaseCost),
                new IntVar("LeaveCost", BaseCost),
                new IntVar("GoldReward", GoldReward)
            };
        }
    }

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
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(ContinueToAsk),
                "KNOWING_SKULL.pages.INITIAL.options.CONTINUE",
                Array.Empty<IHoverTip>())
        };
    }

    private Task ContinueToAsk()
    {
        SetAskState(L10NLookup("KNOWING_SKULL.pages.ASK.description"));
        return Task.CompletedTask;
    }

    private void SetAskState(LocString description)
    {
        UpdateDynamicVars();
        SetEventState(description, new EventOption[]
        {
            new EventOption(this, new Func<Task>(PotionOption),
                "KNOWING_SKULL.pages.ASK.options.POTION",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(GoldOption),
                "KNOWING_SKULL.pages.ASK.options.GOLD",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(CardOption),
                "KNOWING_SKULL.pages.ASK.options.CARD",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(LeaveOption),
                "KNOWING_SKULL.pages.ASK.options.LEAVE",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task PotionOption()
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

        SetAskState(L10NLookup("KNOWING_SKULL.pages.POTION.description"));
    }

    private async Task GoldOption()
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

        SetAskState(L10NLookup("KNOWING_SKULL.pages.GOLD.description"));
    }

    private async Task CardOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _cardCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        _cardCost++;

        // Colorless uncommon card
        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Uncommon)
            .ToList();
        
        var chosenCard = Owner.RunState.Rng.Niche.NextItem(colorlessCards);
        var card = Owner.RunState.CreateCard(chosenCard, Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
      //  await Cmd.Wait(0.75f);

        SetAskState(L10NLookup("KNOWING_SKULL.pages.CARD.description"));
    }

    private async Task LeaveOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            _leaveCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(L10NLookup("KNOWING_SKULL.pages.LEAVE.description"));
    }
}