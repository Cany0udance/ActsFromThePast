using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlimeBossBoss : CustomEncounterModel
{
    public SlimeBossBoss() : base(RoomType.Boss)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;

    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/slime_boss";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<SlimeBoss>(),
                ModelDb.Monster<AcidSlimeLarge>(),
                ModelDb.Monster<SpikeSlimeLarge>(),
                ModelDb.Monster<AcidSlimeMedium>(),
                ModelDb.Monster<SpikeSlimeMedium>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlimeBoss>().ToMutable(), null)
        };
    }
}