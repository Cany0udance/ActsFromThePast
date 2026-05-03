using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class FaceOfCleric : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new MaxHpVar(1M)
    };

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
    }
}