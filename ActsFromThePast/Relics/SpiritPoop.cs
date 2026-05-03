using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class SpiritPoop : CustomRelicModel
{

    public override RelicRarity Rarity => RelicRarity.Event;
    
}