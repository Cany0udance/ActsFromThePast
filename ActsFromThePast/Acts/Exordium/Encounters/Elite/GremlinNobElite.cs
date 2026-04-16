using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinNobElite : CustomEncounterModel
{
    
    public GremlinNobElite() : base(RoomType.Elite)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    
    public override RoomType RoomType => RoomType.Elite;
    
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