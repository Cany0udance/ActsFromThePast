using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Enchantments;

public sealed class Fearful : CustomEnchantmentModel
{
    private const int VulnerableAmount = 1;

    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override bool CanEnchant(CardModel card) =>
        base.CanEnchant(card) && card.Type == CardType.Skill && card.GainsBlock;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return new IHoverTip[]
            {
                HoverTipFactory.FromPower<VulnerablePower>()
            };
        }
    }

    public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props)
    {
        return !props.IsPoweredCardOrMonsterMoveBlock() ? 1M : 3M;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status != EnchantmentStatus.Normal)
            return;

        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            Card.Owner.Creature,
            (decimal)VulnerableAmount,
            Card.Owner.Creature,
            Card);
    }
}