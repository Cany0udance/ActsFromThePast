using ActsFromThePast.Cards;
using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
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

public sealed class Mushrooms : EventModel
{
    private const decimal HEAL_PERCENT = 0.25M;

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<ThreeFungiBeastsEvent>();

    public override bool IsAllowed(RunState runState) =>
        runState.TotalFloor >= 6;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("HealAmount", 0)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars["HealAmount"].BaseValue =
            Math.Floor(Owner.Creature.MaxHp * HEAL_PERCENT);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(this, FightOption,
                "MUSHROOMS.pages.INITIAL.options.FIGHT"),
            new EventOption(this, EatOption,
                "MUSHROOMS.pages.INITIAL.options.EAT",
                HoverTipFactory.FromCardWithCardHoverTips<Parasite>())
        };
    }

    private Task FightOption()
    {
        MushroomPatches.RevealEnemies();

        SetEventState(
            L10NLookup("MUSHROOMS.pages.FIGHT.description"),
            new[]
            {
                new EventOption(this, EnterFightOption,
                    "MUSHROOMS.pages.FIGHT.options.ENTER_COMBAT")
            }
        );
        return Task.CompletedTask;
    }

    private Task EnterFightOption()
    {
        var mushroomRelic = ModelDb.Relic<OddMushroom>().ToMutable();
        var rewards = new List<Reward>
        {
            new RelicReward(mushroomRelic, Owner)
        };

        EnterCombatWithoutExitingEvent<ThreeFungiBeastsEvent>(rewards, false);

        return Task.CompletedTask;
    }

    private async Task EatOption()
    {
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars["HealAmount"].BaseValue);
        await CardPileCmd.AddCurseToDeck<Parasite>(Owner);

        SetEventFinished(
            L10NLookup("MUSHROOMS.pages.EAT.description"));
    }
}