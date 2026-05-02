using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Enchantments;

public sealed class Haunted : CustomEnchantmentModel
{
    private const int IntangibleAmount = 1;
    private const int RingingAmount = 1;

    public override bool HasExtraCardText => true;
    public override bool ShowAmount => false;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return new IHoverTip[]
            {
                HoverTipFactory.FromPower<RingingPower>(),
                HoverTipFactory.FromPower<IntangiblePower>()
            };
        }
    }
    
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Power;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != Card || Status != EnchantmentStatus.Normal)
            return;

        await PowerCmd.Apply<RingingPower>(
            new ThrowingPlayerChoiceContext(),
            Card.Owner.Creature,
            (decimal)RingingAmount,
            Card.Owner.Creature,
            Card);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status != EnchantmentStatus.Normal)
            return;

        await PowerCmd.Apply<IntangiblePower>(
            new ThrowingPlayerChoiceContext(),
            Card.Owner.Creature,
            (decimal)IntangibleAmount,
            Card.Owner.Creature,
            Card);
    }
}