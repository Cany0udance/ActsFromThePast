using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaversElite : CustomEncounterModel
{
    public SlaversElite() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
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