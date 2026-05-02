using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
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
        if (ActsFromThePastConfig.RebalancedMode)
        {
            SetEventState(
                PageDescription("POST_SLAVERS"),
                new[]
                {
                    Option(FightAgain, "POST_SLAVERS"),
                    Option(FleeRebalanced, "POST_SLAVERS_REBALANCED")
                });
        }
        else
        {
            SetEventState(
                PageDescription("POST_SLAVERS"),
                new[]
                {
                    Option(FightAgain, "POST_SLAVERS"),
                    Option(Flee, "POST_SLAVERS")
                });
        }
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
    
    private async Task FleeRebalanced()
    {
        var upgradedCards = Owner.Deck.Cards.Where(c => c.IsUpgraded).ToList();
        if (upgradedCards.Count > 0)
        {
            var card = Rng.NextItem(upgradedCards);
            CardCmd.Downgrade(card);
            CardCmd.Preview(card, style: CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }
        await CreatureCmd.GainMaxHp(Owner.Creature, 5);
        SetEventFinished(PageDescription("FLEE_REBALANCED"));
    }
}