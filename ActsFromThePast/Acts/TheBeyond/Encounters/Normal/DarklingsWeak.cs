using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public class DarklingsWeak : CustomEncounterModel
{
    public DarklingsWeak() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    public override bool IsWeak => true;


    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Darkling>();
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var darklings = new List<(MonsterModel, string?)>();
        for (int i = 0; i < 3; i++)
        {
            var darkling = (Darkling)ModelDb.Monster<Darkling>().ToMutable();
            darkling.SlotIndex = i;
            darklings.Add((darkling, null));
        }
        return darklings;
    }
}