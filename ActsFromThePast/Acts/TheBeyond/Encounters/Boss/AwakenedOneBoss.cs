using ActsFromThePast.Acts.TheBeyond.Enemies;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class AwakenedOneBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/awakened_one";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<AwakenedOne>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Cultist>().ToMutable(), null),
            (ModelDb.Monster<Cultist>().ToMutable(), null),
            (ModelDb.Monster<AwakenedOne>().ToMutable(), null)
        };
    }
}