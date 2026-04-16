using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Necronomicurse : CustomCardModel
{
    public Necronomicurse() : base(
        baseCost: -1,
        type: CardType.Curse,
        rarity: CardRarity.Curse,
        target: TargetType.None)
    {
    }
    
    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new CardKeyword[]
            {
                CardKeyword.Unplayable,
                CardKeyword.Eternal,
            };
        }
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != this)
            return;
        var necronomicon = Owner.Relics.FirstOrDefault(r => r is Necronomicon);
        necronomicon?.Flash();
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
    }
}