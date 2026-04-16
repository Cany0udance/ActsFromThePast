using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class DeadAdventurer : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    private const int GoldReward = 30;
    private const int EncounterChanceStart = 35;
    private const int EncounterChanceRamp = 25;

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter => _enemyType switch
    {
        0 => ModelDb.Encounter<DeadAdventurerSentries>(),
        1 => ModelDb.Encounter<DeadAdventurerGremlinNob>(),
        _ => ModelDb.Encounter<DeadAdventurerLagavulin>()
    };

    private int _encounterChance;
    private int _numSearches;
    private int _enemyType;
    private List<RewardType> _rewards = new();

    internal static bool CombatActive { get; private set; }

    private enum RewardType { Gold, Nothing, Relic }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("EncounterChance", EncounterChanceStart)
    };

    public override void CalculateVars()
    {
        _encounterChance = EncounterChanceStart;
        _numSearches = 0;
        _enemyType = Rng.NextInt(3);

        _rewards = new List<RewardType>
            { RewardType.Gold, RewardType.Nothing, RewardType.Relic };
        for (int i = _rewards.Count - 1; i > 0; i--)
        {
            int j = Rng.NextInt(i + 1);
            (_rewards[i], _rewards[j]) = (_rewards[j], _rewards[i]);
        }

        DynamicVars["EncounterChance"].BaseValue = _encounterChance;
    }

    public override LocString InitialDescription
    {
        get
        {
            var key = _enemyType switch
            {
                0 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.SENTRIES",
                1 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.NOB",
                _ => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.LAGAVULIN"
            };
            return L10NLookup(key);
        }
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Search),
            Option(Leave)
        };
    }

    private async Task Search()
    {
        if (Rng.NextInt(100) < _encounterChance)
        {
            await TriggerCombat();
        }
        else
        {
            await GrantReward();
        }
    }

    private Task TriggerCombat()
    {
        CombatActive = true;
        DeadAdventurerPatches.RevealEnemies();
        SetEventState(
            PageDescription("FIGHT"),
            new[] { Option(EnterCombat, "FIGHT") });
        return Task.CompletedTask;
    }

    private Task EnterCombat()
    {
        var rewards = new List<Reward>
        {
            new CardReward(
                CardCreationOptions.ForRoom(Owner, RoomType.Elite),
                3, Owner),
            new GoldReward(25, 35, Owner)
        };

        foreach (var rewardType in _rewards)
        {
            switch (rewardType)
            {
                case RewardType.Gold:
                    rewards.Add(new GoldReward(GoldReward, GoldReward, Owner));
                    break;
                case RewardType.Relic:
                    var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
                    rewards.Add(new RelicReward(relic, Owner));
                    break;
            }
        }

        EnterCombatWithoutExitingEvent(
            CanonicalEncounter.ToMutable(),
            rewards,
            false);
        return Task.CompletedTask;
    }

    private async Task GrantReward()
    {
        _numSearches++;
        _encounterChance += EncounterChanceRamp;
        DynamicVars["EncounterChance"].BaseValue = _encounterChance;

        var reward = _rewards[0];
        _rewards.RemoveAt(0);
        bool wasLastReward = _numSearches >= 3;

        switch (reward)
        {
            case RewardType.Gold:
                await PlayerCmd.GainGold(GoldReward, Owner);
                break;
            case RewardType.Nothing:
                break;
            case RewardType.Relic:
                var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
                await RelicCmd.Obtain(relic, Owner);
                break;
        }

        if (wasLastReward)
        {
            SetEventFinished(PageDescription("SUCCESS"));
        }
        else
        {
            var pageKey = reward switch
            {
                RewardType.Gold => "GOLD",
                RewardType.Nothing => "NOTHING",
                RewardType.Relic => "RELIC",
                _ => "NOTHING"
            };
            SetEventState(PageDescription(pageKey), GetPostRewardOptions());
        }
    }

    private IReadOnlyList<EventOption> GetPostRewardOptions()
    {
        if (_numSearches >= 3)
        {
            return new[] { Option(Leave, "SUCCESS") };
        }

        return new[]
        {
            Option(Search),
            Option(Leave)
        };
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    protected override void OnEventFinished()
    {
        CombatActive = false;
    }
}