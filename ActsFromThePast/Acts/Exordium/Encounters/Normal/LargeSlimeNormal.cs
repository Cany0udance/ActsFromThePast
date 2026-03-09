using Godot;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LargeSlimeNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Slimes];
    public override bool IsWeak => false;
    
    private static MonsterModel[] LargeSlimes => new MonsterModel[]
    {
        ModelDb.Monster<AcidSlimeLarge>(),
        ModelDb.Monster<SpikeSlimeLarge>()
    };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<AcidSlimeLarge>();
            yield return ModelDb.Monster<SpikeSlimeLarge>();
            yield return ModelDb.Monster<AcidSlimeMedium>();
            yield return ModelDb.Monster<SpikeSlimeMedium>();
        }
    }
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextItem(LargeSlimes).ToMutable(), null)
        };
    }
}