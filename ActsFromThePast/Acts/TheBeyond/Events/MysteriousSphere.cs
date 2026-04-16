using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MysteriousSphere : CustomEventModel
{
    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;
    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<TwoOrbWalkersEvent>();

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Open),
            Option(Leave)
        };
    }

    private Task Open()
    {
        MysteriousSpherePatches.SwapToOpenSphere();
        SetEventState(PageDescription("PRE_COMBAT"), new[]
        {
            Option(Fight, "PRE_COMBAT")
        });
        return Task.CompletedTask;
    }

    private Task Fight()
    {
        var rareRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
        var rewards = new List<Reward>
        {
            new GoldReward(45, 55, Owner),
            new RelicReward(rareRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<TwoOrbWalkersEvent>(rewards, false);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}