using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class CollectorBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/collector";
    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[]
    {
        "torch1", "torch2", "collector"
    };

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Collector>();
            yield return ModelDb.Monster<TorchHead>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Collector>().ToMutable(), "collector")
        };
    }
}