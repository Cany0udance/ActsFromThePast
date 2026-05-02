using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class BloodBank : CustomRelicModel
{
    private const int GoldPerExcessHp = 10;
    private int _hpBeforeRest;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GoldPerHp", GoldPerExcessHp)
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom)
            _hpBeforeRest = Owner.Creature.CurrentHp;
    }

    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner)
            return;

        var actualHealed = Owner.Creature.CurrentHp - _hpBeforeRest;
        var intendedHeal = (int)HealRestSiteOption.GetHealAmount(player);
        var excess = intendedHeal - actualHealed;
        if (excess > 0)
        {
            Flash();
            await PlayerCmd.GainGold(excess * GoldPerExcessHp, Owner);
        }
    }
}