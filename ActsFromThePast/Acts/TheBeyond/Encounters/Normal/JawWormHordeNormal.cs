using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class JawWormHordeNormal : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<JawWorm>(); }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var worm0 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();
        var worm1 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();
        var worm2 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();

        worm0.HardMode = true;
        worm1.HardMode = true;
        worm2.HardMode = true;

        return new List<(MonsterModel, string?)>
        {
            (worm0, null),
            (worm1, null),
            (worm2, null)
        };
    }
}