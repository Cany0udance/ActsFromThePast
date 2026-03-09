using Godot;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LotsOfSlimesNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Slimes];
    public override bool IsWeak => false;
    
    private static MonsterModel[] SmallSlimes => new MonsterModel[]
    {
        ModelDb.Monster<SpikeSlimeSmall>(),
        ModelDb.Monster<AcidSlimeSmall>()
    };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => SmallSlimes;
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // Pool: 3 Spike, 2 Acid - pick without replacement
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<AcidSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<AcidSlimeSmall>().ToMutable()
        };
        
        var result = new List<(MonsterModel, string?)>();
        while (pool.Count > 0)
        {
            var index = Rng.NextInt(pool.Count);
            result.Add((pool[index](), null));
            pool.RemoveAt(index);
        }
        
        return result;
    }
}