using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GuardianBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/guardian";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<Guardian>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Guardian>().ToMutable(), null)
        };
    }
}