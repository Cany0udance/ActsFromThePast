using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeCultistsNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    // TODO make scene for this

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Cultist>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Cultist>().ToMutable(), null),
            (ModelDb.Monster<Cultist>().ToMutable(), null),
            (ModelDb.Monster<Cultist>().ToMutable(), null)
        };
    }
}