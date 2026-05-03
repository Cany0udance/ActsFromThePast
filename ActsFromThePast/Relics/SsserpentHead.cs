using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class SsserpentHead : CustomRelicModel
{
    private const int Gold = 50;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(Gold)
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner.Creature.IsDead)
            return;

        var currentMapPoint = Owner.RunState.CurrentMapPoint;
        if (currentMapPoint == null || currentMapPoint.PointType != MapPointType.Unknown)
            return;

        Flash();
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
    }
}