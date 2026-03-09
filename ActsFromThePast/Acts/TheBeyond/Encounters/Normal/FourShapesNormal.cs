using ActsFromThePast.Acts.TheBeyond.Enemies;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class FourShapesNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>(); // TODO shapes
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Repulsor>();
            yield return ModelDb.Monster<Exploder>();
            yield return ModelDb.Monster<Spiker>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<Repulsor>().ToMutable(),
            () => ModelDb.Monster<Repulsor>().ToMutable(),
            () => ModelDb.Monster<Exploder>().ToMutable(),
            () => ModelDb.Monster<Exploder>().ToMutable(),
            () => ModelDb.Monster<Spiker>().ToMutable(),
            () => ModelDb.Monster<Spiker>().ToMutable()
        };

        var result = new List<(MonsterModel, string?)>();

        for (int i = 0; i < 4; i++)
        {
            var index = Rng.NextInt(pool.Count);
            result.Add((pool[index](), null));
            pool.RemoveAt(index);
        }

        return result;
    }
}