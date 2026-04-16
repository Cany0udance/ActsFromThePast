using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SmallSlimesWeak : CustomEncounterModel
{
    public SmallSlimesWeak() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Slimes];
    public override bool IsWeak => true;
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<SpikeSlimeSmall>();
            yield return ModelDb.Monster<AcidSlimeSmall>();
            yield return ModelDb.Monster<SpikeSlimeMedium>();
            yield return ModelDb.Monster<AcidSlimeMedium>();
        }
    }
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 50/50: either (Spike Small + Acid Medium) or (Acid Small + Spike Medium)
        if (Rng.NextInt(2) == 0)
        {
            return new List<(MonsterModel, string?)>
            {
                (ModelDb.Monster<SpikeSlimeSmall>().ToMutable(), null),
                (ModelDb.Monster<AcidSlimeMedium>().ToMutable(), null)
            };
        }
        else
        {
            return new List<(MonsterModel, string?)>
            {
                (ModelDb.Monster<AcidSlimeSmall>().ToMutable(), null),
                (ModelDb.Monster<SpikeSlimeMedium>().ToMutable(), null)
            };
        }
    }
}