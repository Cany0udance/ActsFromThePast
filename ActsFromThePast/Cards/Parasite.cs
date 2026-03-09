using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Cards;

public sealed class Parasite : CardModel
{
    public Parasite() : base(
        canonicalEnergyCost: -1,
        type: CardType.Curse,
        rarity: CardRarity.Curse,
        targetType: TargetType.None)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new CardKeyword[]
            {
                CardKeyword.Unplayable
            };
        }
    }

    public override int MaxUpgradeLevel => 0;

    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card != this)
            return;
    
        if (Owner?.Creature == null)
            return;
    
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, 3, false);
        ModAudio.Play("general", "blood_swish");
    }
}