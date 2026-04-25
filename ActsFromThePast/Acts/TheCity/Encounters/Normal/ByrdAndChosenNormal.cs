using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ByrdAndChosenNormal : CustomEncounterModel
{
    public ByrdAndChosenNormal() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Byrd>();
            yield return ModelDb.Monster<Chosen>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Byrd>().ToMutable(), null),
            (ModelDb.Monster<Chosen>().ToMutable(), null)
        };
    }
    
    public override IEnumerable<string> ExtraAssetPaths => new[]
    {
        "res://scenes/vfx/vfx_fire_burst.tscn"
    };
}