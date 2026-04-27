using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Enchantments;

public sealed class BurnBright : CustomEnchantmentModel
{
    private const int MaxCombats = 5;
    private int _combatsSeen;

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Combats", MaxCombats),
    ];

    [SavedProperty]
    public int CombatsSeen
    {
        get => _combatsSeen;
        set
        {
            AssertMutable();
            _combatsSeen = value;
            DynamicVars["Combats"].BaseValue = MaxCombats - _combatsSeen;
        }
    }

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return !props.IsPoweredAttack() ? 1M : 2M;
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        var pile = Card.Pile;
        if (pile == null || pile.Type != PileType.Deck)
            return;

        CombatsSeen++;

        if (CombatsSeen >= MaxCombats && Card.Pile.Type == PileType.Deck)
            await CardPileCmd.RemoveFromDeck(Card);
    }
}