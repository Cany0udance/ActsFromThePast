using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class FourShapesNormal : CustomEncounterModel
{
    public FourShapesNormal() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Shapes];
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