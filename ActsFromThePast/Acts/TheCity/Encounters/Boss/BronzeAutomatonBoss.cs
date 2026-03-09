using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class BronzeAutomatonBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/bronze_automaton";
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => new[]
    {
        "orb1", "orb2", "automaton"
    };
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<BronzeAutomaton>();
            yield return ModelDb.Monster<BronzeOrb>();
        }
    }
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<BronzeAutomaton>().ToMutable(), "automaton")
        };
    }
}