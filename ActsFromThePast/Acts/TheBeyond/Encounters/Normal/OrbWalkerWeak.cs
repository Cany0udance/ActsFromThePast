using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class OrbWalkerWeak : CustomEncounterModel
{
    public OrbWalkerWeak() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    
    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<OrbWalker>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> 
        { 
            (ModelDb.Monster<OrbWalker>().ToMutable(), null) 
        };
    }
    
    public override IEnumerable<string> ExtraAssetPaths => new[]
    {
        "res://scenes/vfx/vfx_fire_burst.tscn"
    };
}