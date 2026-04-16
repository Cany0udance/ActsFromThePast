using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Colosseum : CustomEventModel
{
    private enum FightPhase { Slavers, Nobs }
    private FightPhase _lastFight;
    public static bool NeedsReplayFix;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };
    public override bool IsShared => true;
    public override bool IsAllowed(IRunState runState) =>
        runState.TotalFloor >= 23 && runState.Players.Count == 1;

    public override void OnRoomEnter()
    {
        NeedsReplayFix = false;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Enter) };
    }

    private Task Enter()
    {
        SetEventState(
            PageDescription("FIGHT_INTRO"),
            new[] { Option(Fight, "FIGHT_INTRO") });
        return Task.CompletedTask;
    }

    private Task Fight()
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
            PageDescription("POST_SLAVERS"),
            new[]
            {
                Option(FightAgain, "POST_SLAVERS"),
                Option(Flee, "POST_SLAVERS")
            });
        return Task.CompletedTask;
    }

    private Task FightAgain()
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

    private Task Flee()
    {
        SetEventFinished(PageDescription("FLEE"));
        return Task.CompletedTask;
    }
}