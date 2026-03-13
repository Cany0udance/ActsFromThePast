using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class CityProtectorsNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Sentry>();
            yield return ModelDb.Monster<SphericGuardian>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var sentry = (Sentry)ModelDb.Monster<Sentry>().ToMutable();
        sentry.BoltFirst = true;

        return new List<(MonsterModel, string?)>
        {
            (sentry, null),
            (ModelDb.Monster<SphericGuardian>().ToMutable(), null)
        };
    }
}