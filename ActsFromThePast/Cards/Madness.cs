using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Cards;

public sealed class Madness : CardModel
{
    public Madness() : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new[] { CardKeyword.Exhaust };
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.CostsEnergyOrStars(false) || c.CostsEnergyOrStars(true))
            .ToList();

        if (hand.Count == 0)
            return;

        var target = Owner.RunState.Rng.CombatCardSelection.NextItem(hand);
        target?.SetToFreeThisCombat();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}