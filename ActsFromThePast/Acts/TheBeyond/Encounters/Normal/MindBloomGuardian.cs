using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class MindBloomGuardian : CustomEncounterModel
{
    public MindBloomGuardian() : base(RoomType.Monster)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<Guardian>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Guardian>().ToMutable(), null)
        };
    }
}