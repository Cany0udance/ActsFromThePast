using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaversElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => new[] { "blue", "taskmaster", "red" };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<SlaverBlue>();
            yield return ModelDb.Monster<Taskmaster>();
            yield return ModelDb.Monster<SlaverRed>();
        }
    }
    
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlaverBlue>().ToMutable(), "blue"),
            (ModelDb.Monster<Taskmaster>().ToMutable(), "taskmaster"),
            (ModelDb.Monster<SlaverRed>().ToMutable(), "red")
        };
    }
}