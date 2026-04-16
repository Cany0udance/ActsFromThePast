using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class AwakenedOneBoss : CustomEncounterModel
{
    public AwakenedOneBoss() : base(RoomType.Boss)
    {
    }
    
    public override bool IsValidForAct(ActModel act) => act is TheBeyondAct;
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/awakened_one";
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => new[] { "cultist_left", "cultist_right", "awakened" };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<AwakenedOne>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Cultist>().ToMutable(), "cultist_left"),
            (ModelDb.Monster<Cultist>().ToMutable(), "cultist_right"),
            (ModelDb.Monster<AwakenedOne>().ToMutable(), "awakened")
        };
    }
}