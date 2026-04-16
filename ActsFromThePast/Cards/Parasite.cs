using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Parasite : CustomCardModel
{
    public Parasite() : base(
        baseCost: -1,
        type: CardType.Curse,
        rarity: CardRarity.Curse,
        target: TargetType.None)
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