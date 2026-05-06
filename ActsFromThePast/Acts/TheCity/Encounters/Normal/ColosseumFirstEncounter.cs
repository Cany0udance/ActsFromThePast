using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ColosseumFirstEncounter : CustomEncounterModel
{
    public ColosseumFirstEncounter() : base(RoomType.Monster)
    {
    }
    public override bool IsValidForAct(ActModel act) => false;
    public override bool ShouldGiveRewards => false;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => new[] { "blue", "red" };
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<SlaverBlue>();
            yield return ModelDb.Monster<SlaverRed>();
        }
    }
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlaverBlue>().ToMutable(), "blue"),
            (ModelDb.Monster<SlaverRed>().ToMutable(), "red"),
        };
    }
}