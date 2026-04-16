using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
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

public sealed class MaskedBandits : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    public static bool WaitingForMapEasterEgg;

    internal static bool CombatActive { get; private set; }

    public override bool IsShared => true;

    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<RedMaskBanditsEvent>();

    public override bool IsAllowed(IRunState runState) =>
        runState.TotalFloor >= 23 &&
        !runState.Players.Any(p => p.Relics.Any(r => r is RedMask));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Pay),
            Option(Fight)
        };
    }

    public override void OnRoomEnter()
    {
        WaitingForMapEasterEgg = false;
    }

    private async Task Pay()
    {
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Stolen);

        SetEventState(PageDescription("PAID_1"), new[]
        {
            new EventOption(this, Paid2,
                $"{Id.Entry}.pages.PAID_1.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
    }

    private Task Paid2()
    {
        SetEventState(PageDescription("PAID_2"), new[]
        {
            new EventOption(this, Paid3,
                $"{Id.Entry}.pages.PAID_2.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }

    private Task Paid3()
    {
        WaitingForMapEasterEgg = true;
        SetEventFinished(PageDescription("PAID_3"));
        return Task.CompletedTask;
    }

    private Task Fight()
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