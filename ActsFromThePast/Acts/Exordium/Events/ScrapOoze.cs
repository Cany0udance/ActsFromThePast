using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ScrapOoze : EventModel
{
    private const int BaseDamage = 5;
    private const int BaseRelicChance = 25;
    private const int ChanceIncreasePerAttempt = 10;
    private const int DamageIncreasePerAttempt = 1;

    private int _attempts;

    private int Attempts
    {
        get => _attempts;
        set
        {
            AssertMutable();
            _attempts = value;
        }
    }

    private int CurrentDamage => BaseDamage + Attempts * DamageIncreasePerAttempt;
    private int CurrentRelicChance => BaseRelicChance + Attempts * ChanceIncreasePerAttempt;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Damage", CurrentDamage),
                new IntVar("RelicChance", CurrentRelicChance)
            };
        }
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "scrap_ooze");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(ReachInsideOption),
                "SCRAP_OOZE.pages.INITIAL.options.REACH",
                Array.Empty<IHoverTip>()).ThatDoesDamage(CurrentDamage),
            new EventOption(this, new Func<Task>(LeaveOption),
                "SCRAP_OOZE.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task ReachInsideOption()
    {
        // TODO poison sound
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            CurrentDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        int roll = Rng.NextInt(100);
        int threshold = 100 - CurrentRelicChance;
        Log.Info($"[ScrapOoze] Roll: {roll}, Threshold: {threshold}, RelicChance: {CurrentRelicChance}%, Success: {roll >= threshold}");

        if (roll >= threshold)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
            await RelicCmd.Obtain(relic, Owner);
            SetEventFinished(L10NLookup("SCRAP_OOZE.pages.SUCCESS.description"));
        }
        else
        {
            Attempts++;
            DynamicVars["Damage"].BaseValue = CurrentDamage;
            DynamicVars["RelicChance"].BaseValue = CurrentRelicChance;

            SetEventState(L10NLookup("SCRAP_OOZE.pages.FAIL.description"), new EventOption[]
            {
                new EventOption(this, new Func<Task>(ReachInsideOption),
                    "SCRAP_OOZE.pages.FAIL.options.REACH",
                    Array.Empty<IHoverTip>()).ThatDoesDamage(CurrentDamage),
                new EventOption(this, new Func<Task>(LeaveOption),
                    "SCRAP_OOZE.pages.FAIL.options.LEAVE",
                    Array.Empty<IHoverTip>())
            });
        }
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("SCRAP_OOZE.pages.LEAVE.description"));
    }
}