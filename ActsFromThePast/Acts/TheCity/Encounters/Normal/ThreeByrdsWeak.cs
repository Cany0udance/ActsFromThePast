using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeByrdsWeak : CustomEncounterModel
{
    public ThreeByrdsWeak() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();
    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Byrd>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Byrd>().ToMutable(), null),
            (ModelDb.Monster<Byrd>().ToMutable(), null),
            (ModelDb.Monster<Byrd>().ToMutable(), null)
        };
    }
}