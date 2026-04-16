using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LagavulinElite : CustomEncounterModel
{
    
    public LagavulinElite() : base(RoomType.Elite)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Lagavulin>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Lagavulin>().ToMutable(), null)
        };
    }
}