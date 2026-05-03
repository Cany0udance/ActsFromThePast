using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Pain : CustomCardModel
{
    public Pain() : base(
        baseCost: -1,
        type: CardType.Curse,
        rarity: CardRarity.Curse,
        target: TargetType.None)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Unplayable
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(1M)
    };

    public override int MaxUpgradeLevel => 0;

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
            return;

        var hand = PileType.Hand.GetPile(Owner);
        if (!hand.Cards.Contains(this))
            return;

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this);
    }
}