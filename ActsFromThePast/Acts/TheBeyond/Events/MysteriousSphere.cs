using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Patches.RoomEvents;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MysteriousSphere : EventModel
{
    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;
    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<TwoOrbWalkersEvent>();

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(this, OpenOption,
                "MYSTERIOUS_SPHERE.pages.INITIAL.options.OPEN"),
            new EventOption(this, LeaveOption,
                "MYSTERIOUS_SPHERE.pages.INITIAL.options.LEAVE")
        };
    }

    private Task OpenOption()
    {
        MysteriousSpherePatches.SwapToOpenSphere();

        SetEventState(
            L10NLookup("MYSTERIOUS_SPHERE.pages.PRE_COMBAT.description"),
            new[]
            {
                new EventOption(this, FightOption,
                    "MYSTERIOUS_SPHERE.pages.PRE_COMBAT.options.FIGHT")
            }
        );
        return Task.CompletedTask;
    }

    private Task FightOption()
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

    private Task LeaveOption()
    {
        SetEventFinished(
            L10NLookup("MYSTERIOUS_SPHERE.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}