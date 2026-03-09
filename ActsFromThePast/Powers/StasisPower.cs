using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class StasisPower : PowerModel
{
    private CardModel? _stolenCard;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool IsInstanced => true;

    private CardModel? StolenCard
    {
        get => _stolenCard;
        set
        {
            AssertMutable();
            _stolenCard = value;
        }
    }

    private Creature? _cardOwner;
    private Creature? CardOwner
    {
        get => _cardOwner;
        set
        {
            AssertMutable();
            _cardOwner = value;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return StolenCard == null
                ? Array.Empty<IHoverTip>()
                : new[] { HoverTipFactory.FromCard(StolenCard) };
        }
    }

    public async Task Capture(CardModel card, Creature originalOwner)
    {
        StolenCard = card;
        CardOwner = originalOwner;
    }

    public override async Task BeforeDeath(Creature target)
    {
        if (Owner != target || StolenCard == null || CardOwner == null)
            return;

        var combatState = CardOwner.CombatState;
        if (combatState == null)
            return;

        // Clear the removed flag
        var removedProp = typeof(CardModel).GetProperty("HasBeenRemovedFromState", BindingFlags.Public | BindingFlags.Instance);
        removedProp?.SetValue(StolenCard, false);

        // Re-register the card with combat state
        var allCardsField = typeof(CombatState).GetField("_allCards", BindingFlags.NonPublic | BindingFlags.Instance);
        var allCards = allCardsField?.GetValue(combatState) as List<CardModel>;
        if (allCards != null && !allCards.Contains(StolenCard))
        {
            allCards.Add(StolenCard);
        }

        await CardPileCmd.Add(StolenCard, PileType.Hand);
    }
}