using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerLagavulin : CustomEncounterModel
{
    public DeadAdventurerLagavulin() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Lagavulin>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var lagavulin = (Lagavulin)ModelDb.Monster<Lagavulin>().ToMutable();
        lagavulin.StartsAwake = true;
        return new List<(MonsterModel, string?)>
        {
            (lagavulin, null)
        };
    }
}
