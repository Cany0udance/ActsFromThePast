using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinLeaderElite : CustomEncounterModel
{
    public GremlinLeaderElite() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();
    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[]
    {
        "gremlin1", "gremlin2", "gremlin3", "leader"
    };

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<GremlinLeader>();
            yield return ModelDb.Monster<GremlinMad>();
            yield return ModelDb.Monster<GremlinSneaky>();
            yield return ModelDb.Monster<GremlinFat>();
            yield return ModelDb.Monster<GremlinShield>();
            yield return ModelDb.Monster<GremlinWizard>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<GremlinMad>().ToMutable(),
            () => ModelDb.Monster<GremlinMad>().ToMutable(),
            () => ModelDb.Monster<GremlinSneaky>().ToMutable(),
            () => ModelDb.Monster<GremlinSneaky>().ToMutable(),
            () => ModelDb.Monster<GremlinFat>().ToMutable(),
            () => ModelDb.Monster<GremlinFat>().ToMutable(),
            () => ModelDb.Monster<GremlinShield>().ToMutable(),
            () => ModelDb.Monster<GremlinWizard>().ToMutable()
        };

        var result = new List<(MonsterModel, string?)>();

        // Fill gremlin slots from back to front (closest to leader first)
        var gremlinSlots = Slots.Where(s => s != "leader").Reverse().ToList();

        for (int i = 0; i < 2; i++)
        {
            var index = Rng.NextInt(pool.Count);
            result.Add((pool[index](), gremlinSlots[i]));
            pool.RemoveAt(index);
        }

        result.Add((ModelDb.Monster<GremlinLeader>().ToMutable(), "leader"));

        return result;
    }
}