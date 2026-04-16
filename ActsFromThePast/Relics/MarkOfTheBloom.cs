using BaseLib.Abstracts;
using BaseLib.Hooks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class MarkOfTheBloom : CustomRelicModel, IHealAmountModifier
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public decimal ModifyHealMultiplicative(Creature creature, decimal amount)
    {
        if (creature.Player != Owner)
            return 1M;
        if (amount > 0)
            Flash();
        return 0M;
    }
}