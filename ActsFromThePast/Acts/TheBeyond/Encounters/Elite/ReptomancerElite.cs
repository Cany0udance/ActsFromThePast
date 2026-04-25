using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class ReptomancerElite : CustomEncounterModel
{
    public ReptomancerElite() : base(RoomType.Elite)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[]
    {
        "dagger3", "dagger1", "reptomancer", "dagger2", "dagger4"
    };

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Reptomancer>();
            yield return ModelDb.Monster<SnakeDagger>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var result = new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SnakeDagger>().ToMutable(), "dagger3"),
            (ModelDb.Monster<SnakeDagger>().ToMutable(), "dagger4"),
            (ModelDb.Monster<Reptomancer>().ToMutable(), "reptomancer")
        };

        return result;
    }
}