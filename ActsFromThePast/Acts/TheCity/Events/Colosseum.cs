using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Colosseum : EventModel
{
    private enum FightPhase { Slavers, Nobs }
    private FightPhase _lastFight;
    public static bool NeedsReplayFix;

    public override bool IsShared => true;

    public override bool IsAllowed(RunState runState) =>
        runState.TotalFloor >= 23;
    
    public override void OnRoomEnter()
    {
        NeedsReplayFix = false;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(this, IntroOption,
                "COLOSSEUM.pages.INITIAL.options.ENTER")
        };
    }

    private Task IntroOption()
    {
        SetEventState(
            L10NLookup("COLOSSEUM.pages.FIGHT_INTRO.description"),
            new[]
            {
                new EventOption(this, StartSlaverFight,
                    "COLOSSEUM.pages.FIGHT_INTRO.options.FIGHT")
            }
        );
        return Task.CompletedTask;
    }

    private Task StartSlaverFight()
    {
        _lastFight = FightPhase.Slavers;
        EnterCombatWithoutExitingEvent(
            ModelDb.Encounter<ColosseumFirstEncounter>().ToMutable(),
            Array.Empty<Reward>(),
            true);
        return Task.CompletedTask;
    }

    public override Task Resume(AbstractRoom room)
    {
        SetEventState(
            L10NLookup("COLOSSEUM.pages.POST_SLAVERS.description"),
            new[]
            {
                new EventOption(this, StartNobFight,
                    "COLOSSEUM.pages.POST_SLAVERS.options.FIGHT"),
                new EventOption(this, FleeOption,
                    "COLOSSEUM.pages.POST_SLAVERS.options.FLEE")
            }
        );
        return Task.CompletedTask;
    }


    private Task StartNobFight()
    {
        NeedsReplayFix = true;

        var rareRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
        var uncommonRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Uncommon).ToMutable();

        var rewards = new List<Reward>
        {
            new RelicReward(rareRelic, Owner),
            new RelicReward(uncommonRelic, Owner),
            new GoldReward(100, Owner)
        };

        EnterCombatWithoutExitingEvent(
            ModelDb.Encounter<ColosseumSecondEncounter>().ToMutable(),
            rewards,
            false);
        return Task.CompletedTask;
    }

    private Task FleeOption()
    {
        SetEventFinished(
            L10NLookup("COLOSSEUM.pages.FLEE.description"));
        return Task.CompletedTask;
    }
}