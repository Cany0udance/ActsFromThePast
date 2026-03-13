using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaverNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    private static MonsterModel[] Slavers => new MonsterModel[]
    {
        ModelDb.Monster<SlaverRed>(),
        ModelDb.Monster<SlaverBlue>()
    };

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<SlaverRed>();
            yield return ModelDb.Monster<SlaverBlue>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextItem(Slavers).ToMutable(), null)
        };
    }
}