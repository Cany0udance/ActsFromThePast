using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class BlueSlaverNormal : EncounterModel
{
    
    // TODO Add tag so blue/red slaver can't be fought back-to-back
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<SlaverBlue>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlaverBlue>().ToMutable(), null)
        };
    }
}