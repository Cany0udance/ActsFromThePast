using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ColosseumSecondEncounter : CustomEncounterModel
{
    public ColosseumSecondEncounter() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Taskmaster>();
            yield return ModelDb.Monster<GremlinNob>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Taskmaster>().ToMutable(), null),
            (ModelDb.Monster<GremlinNob>().ToMutable(), null),
        };
    }
}