using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ChampBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/champ";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<Champ>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Champ>().ToMutable(), null)
        };
    }
}