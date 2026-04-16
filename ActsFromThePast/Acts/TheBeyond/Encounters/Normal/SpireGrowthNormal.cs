using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class SpireGrowthNormal : CustomEncounterModel
{
    public SpireGrowthNormal() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<SpireGrowth>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> 
        { 
            (ModelDb.Monster<SpireGrowth>().ToMutable(), null) 
        };
    }
}