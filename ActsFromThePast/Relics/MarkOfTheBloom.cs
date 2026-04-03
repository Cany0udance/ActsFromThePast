using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Relics;

public sealed class MarkOfTheBloom : RelicModel, IHealAmountModifier
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