using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumWildlifeNormal : CustomEncounterModel
{
    public ExordiumWildlifeNormal() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();
    public override bool IsWeak => false;
    
    private static MonsterModel[] StrongPool => new MonsterModel[]
    {
        ModelDb.Monster<FungiBeast>(),
        ModelDb.Monster<JawWorm>()
    };
    
    private static MonsterModel[] WeakPool => new MonsterModel[]
    {
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>(),
        ModelDb.Monster<SpikeSlimeMedium>(),
        ModelDb.Monster<AcidSlimeMedium>()
    };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => StrongPool.Concat(WeakPool);
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextItem(StrongPool).ToMutable(), null),
            (Rng.NextItem(WeakPool).ToMutable(), null)
        };
    }
}