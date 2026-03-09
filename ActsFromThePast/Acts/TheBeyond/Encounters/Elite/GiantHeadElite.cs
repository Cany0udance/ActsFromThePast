using ActsFromThePast.Acts.TheBeyond.Enemies;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class GiantHeadElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<GiantHead>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<GiantHead>().ToMutable(), null)
        };
    }
}