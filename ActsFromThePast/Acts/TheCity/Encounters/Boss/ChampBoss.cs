using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ChampBoss : CustomEncounterModel
{
    public ChampBoss() : base(RoomType.Boss)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is TheCityAct;
    
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/champ";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<Champ>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Champ>().ToMutable(), null)
        };
    }
}