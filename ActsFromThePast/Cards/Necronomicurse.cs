using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Cards;

public sealed class Necronomicurse : CardModel
{
    public Necronomicurse() : base(
        canonicalEnergyCost: -1,
        type: CardType.Curse,
        rarity: CardRarity.Event,
        targetType: TargetType.None)
    {
    }

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
        // Unplayable, so this should never be called
        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != this)
            return;
        // Flash the Necronomicon if player has it
        var necronomicon = Owner.Relics.FirstOrDefault(r => r is Necronomicon);
        necronomicon?.Flash();
        // Return the same card to hand
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        // Cannot be upgraded
    }
}