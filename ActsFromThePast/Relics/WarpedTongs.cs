using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class WarpedTongs : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != CombatSide.Player)
            return;

        var pile = PileType.Hand.GetPile(Owner);
        var upgradableCards = pile.Cards.Where(c => c.IsUpgradable).ToList();

        if (upgradableCards.Count == 0)
            return;

        Flash();
        var card = Owner.RunState.Rng.CombatCardSelection.NextItem(upgradableCards);
        CardCmd.Upgrade(card);
        // TODO make card flash
    }
}