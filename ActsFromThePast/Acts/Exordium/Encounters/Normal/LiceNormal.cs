using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LiceNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Lice];
    public override bool IsWeak => false;
    
    private static MonsterModel[] Lice => new MonsterModel[]
    {
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>()
    };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => Lice;
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextItem(Lice).ToMutable(), null),
            (Rng.NextItem(Lice).ToMutable(), null),
            (Rng.NextItem(Lice).ToMutable(), null)
        };
    }
}