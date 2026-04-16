using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class SphereAndTwoShapesNormal : CustomEncounterModel
{
    public SphereAndTwoShapesNormal() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Shapes];
    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Spiker>();
            yield return ModelDb.Monster<Repulsor>();
            yield return ModelDb.Monster<Exploder>();
            yield return ModelDb.Monster<SphericGuardian>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (RandomShape(), null),
            (RandomShape(), null),
            (ModelDb.Monster<SphericGuardian>().ToMutable(), null)
        };
    }

    private MonsterModel RandomShape()
    {
        return (Rng.NextInt(3)) switch
        {
            0 => ModelDb.Monster<Spiker>().ToMutable(),
            1 => ModelDb.Monster<Repulsor>().ToMutable(),
            _ => ModelDb.Monster<Exploder>().ToMutable()
        };
    }
}