using ActsFromThePast.Cards;
using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Mushrooms : CustomEventModel
{
    private const decimal HEAL_PERCENT = 0.25M;

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;
    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<ThreeFungiBeastsEvent>();
    public override bool IsAllowed(IRunState runState) =>
        runState.TotalFloor >= 7;

    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HealAmount", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["HealAmount"].BaseValue =
            Math.Floor(Owner.Creature.MaxHp * HEAL_PERCENT);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var eatHoverTips = ActsFromThePastConfig.RebalancedMode
            ? HoverTipFactory.FromCardWithCardHoverTips<SporeMind>().ToArray()
            : HoverTipFactory.FromCardWithCardHoverTips<Parasite>().ToArray();

        return new[]
        {
            Option(Fight),
            Option(Eat, ActsFromThePastConfig.RebalancedMode ? "INITIAL_REBALANCED" : "INITIAL", eatHoverTips)
        };
    }

    private Task Fight()
    {
        MushroomPatches.RevealEnemies();
        SetEventState(
            PageDescription("FIGHT"),
            new[] { Option(EnterCombat, "FIGHT") });
        return Task.CompletedTask;
    }

    private Task EnterCombat()
    {
        var mushroomRelic = ModelDb.Relic<OddMushroom>().ToMutable();
        var rewards = new List<Reward>
        {
            new RelicReward(mushroomRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<ThreeFungiBeastsEvent>(rewards, false);
        return Task.CompletedTask;
    }

    private async Task Eat()
    {
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars["HealAmount"].BaseValue);

        if (ActsFromThePastConfig.RebalancedMode)
            await CardPileCmd.AddCurseToDeck<SporeMind>(Owner);
        else
            await CardPileCmd.AddCurseToDeck<Parasite>(Owner);

        SetEventFinished(PageDescription("EAT"));
    }
}