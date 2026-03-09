using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class MaskedBandits : EventModel
{
    public static bool WaitingForMapEasterEgg;
    internal static bool CombatActive { get; private set; }
    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;
    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<RedMaskBanditsEvent>();
    public override bool IsAllowed(RunState runState) =>
        runState.TotalFloor >= 23 &&
        !runState.Players.Any(p => p.Relics.Any(r => r is RedMask));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(this, PayOption,
                "MASKED_BANDITS.pages.INITIAL.options.PAY"),
            new EventOption(this, FightOption,
                "MASKED_BANDITS.pages.INITIAL.options.FIGHT")
        };
    }
    
    public override void OnRoomEnter()
    {
        WaitingForMapEasterEgg = false;
    }

    private async Task PayOption()
    {
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
        {
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Stolen);
        }

        SetEventState(
            L10NLookup("MASKED_BANDITS.pages.PAID_1.description"),
            new[]
            {
                new EventOption(this, Paid2,
                    "MASKED_BANDITS.pages.PAID_1.options.CONTINUE")
            }
        );
    }

    private Task Paid2()
    {
        SetEventState(
            L10NLookup("MASKED_BANDITS.pages.PAID_2.description"),
            new[]
            {
                new EventOption(this, Paid3,
                    "MASKED_BANDITS.pages.PAID_2.options.CONTINUE")
            }
        );
        return Task.CompletedTask;
    }

    private Task Paid3()
    {
        WaitingForMapEasterEgg = true;
        SetEventFinished(
            L10NLookup("MASKED_BANDITS.pages.PAID_3.description"));
        return Task.CompletedTask;
    }

    private Task FightOption()
    {
        CombatActive = true;
        var redMaskRelic = ModelDb.Relic<RedMask>().ToMutable();
        var rewards = new List<Reward>
        {
            new GoldReward(25, 35, Owner),
            new RelicReward(redMaskRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<RedMaskBanditsEvent>(rewards, false);
        return Task.CompletedTask;
    }

    protected override void OnEventFinished()
    {
        CombatActive = false;
    }
}