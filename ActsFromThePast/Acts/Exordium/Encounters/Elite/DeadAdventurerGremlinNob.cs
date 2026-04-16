using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerGremlinNob : CustomEncounterModel
{
    public DeadAdventurerGremlinNob() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<GremlinNob>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<GremlinNob>().ToMutable(), null)
        };
    }
}