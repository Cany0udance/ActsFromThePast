using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerSentries : CustomEncounterModel
{
    public DeadAdventurerSentries() : base(RoomType.Elite)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Sentry>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var sentry0 = (Sentry)ModelDb.Monster<Sentry>().ToMutable();
        var sentry1 = (Sentry)ModelDb.Monster<Sentry>().ToMutable();
        var sentry2 = (Sentry)ModelDb.Monster<Sentry>().ToMutable();

        sentry0.BoltFirst = true;
        sentry1.BoltFirst = false;
        sentry2.BoltFirst = true;

        return new List<(MonsterModel, string?)>
        {
            (sentry0, null),
            (sentry1, null),
            (sentry2, null)
        };
    }
}