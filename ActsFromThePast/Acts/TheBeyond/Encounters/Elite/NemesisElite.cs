using ActsFromThePast.Acts.TheBeyond.Enemies;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class NemesisElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Nemesis>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Nemesis>().ToMutable(), null)
        };
    }
}