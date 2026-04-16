using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LiceWeak : CustomEncounterModel
{
    public LiceWeak() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Lice];
    public override bool IsWeak => true;
    
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
            (Rng.NextItem(Lice).ToMutable(), null)
        };
    }
}