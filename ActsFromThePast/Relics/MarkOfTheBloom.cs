using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Relics;

public sealed class MarkOfTheBloom : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;
// TODO fix odd fairy slash lizard tail interaction
    public override decimal ModifyHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player != Owner)
            return amount;
        if (amount > 0)
            Flash();
        return 0;
    }
}