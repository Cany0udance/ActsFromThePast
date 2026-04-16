using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumThugsNormal : CustomEncounterModel
{
    public ExordiumThugsNormal() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();
    public override bool IsWeak => false;
    
    private static MonsterModel[] FrontPool => new MonsterModel[]
    {
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>(),
        ModelDb.Monster<SpikeSlimeMedium>(),
        ModelDb.Monster<AcidSlimeMedium>()
    };
    
    private static MonsterModel[] BackPool => new MonsterModel[]
    {
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<SlaverBlue>(),
        ModelDb.Monster<SlaverRed>(),
        ModelDb.Monster<Looter>()
    };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => FrontPool.Concat(BackPool);
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextItem(FrontPool).ToMutable(), null),
            (Rng.NextItem(BackPool).ToMutable(), null)
        };
    }
}