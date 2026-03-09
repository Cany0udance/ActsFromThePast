using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LooterNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Looter>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Looter>().ToMutable(), null)
        };
    }
}