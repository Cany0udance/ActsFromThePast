using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlimeBossBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/slime_boss";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<SlimeBoss>(),
                ModelDb.Monster<AcidSlimeLarge>(),
                ModelDb.Monster<SpikeSlimeLarge>()
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