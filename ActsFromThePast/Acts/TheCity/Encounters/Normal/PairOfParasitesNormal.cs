using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class PairOfParasitesNormal : CustomEncounterModel
{
    public PairOfParasitesNormal() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<ShelledParasite>();
            yield return ModelDb.Monster<FungiBeast>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<ShelledParasite>().ToMutable(), null),
            (ModelDb.Monster<FungiBeast>().ToMutable(), null)
        };
    }
}