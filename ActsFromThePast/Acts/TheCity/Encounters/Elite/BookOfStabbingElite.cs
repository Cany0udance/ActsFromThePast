using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class BookOfStabbingElite : CustomEncounterModel
{
    public BookOfStabbingElite() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<BookOfStabbing>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<BookOfStabbing>().ToMutable(), null)
        };
    }
}