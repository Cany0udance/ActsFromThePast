using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlimeBossBoss : CustomEncounterModel
{
    public SlimeBossBoss() : base(RoomType.Boss)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;

    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/slime_boss";

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[]
    {
        "spike_med_1", "spike_large", "spike_med_2",
        "acid_med_1", "slime_boss", "acid_large", "acid_med_2"
    };

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<SlimeBoss>(),
                ModelDb.Monster<SpikeSlimeLarge>(),
                ModelDb.Monster<SpikeSlimeMedium>(),
                ModelDb.Monster<AcidSlimeLarge>(),
                ModelDb.Monster<AcidSlimeMedium>(),
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlimeBoss>().ToMutable(), "slime_boss")
        };
    }
}