using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class GoldenIdol : EventModel
{
    private const decimal HpLossPercent = 0.35M;
    private const decimal MaxHpLossPercent = 0.10M;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Damage", 0),
                new IntVar("MaxHpLoss", 0)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars["Damage"].BaseValue = Math.Floor(Owner.Creature.MaxHp * HpLossPercent);
        var maxHpLoss = Math.Floor(Owner.Creature.MaxHp * MaxHpLossPercent);
        if (maxHpLoss < 1)
            maxHpLoss = 1;
        DynamicVars["MaxHpLoss"].BaseValue = maxHpLoss;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "golden_idol");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(TakeIdolOption),
                "GOLDEN_IDOL.pages.INITIAL.options.TAKE",
                HoverTipFactory.FromRelic(ModelDb.Relic<GoldenIdolOriginal>())),
            new EventOption(this, new Func<Task>(LeaveOption),
                "GOLDEN_IDOL.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task TakeIdolOption()
    {
        var relic = ModelDb.Relic<GoldenIdolOriginal>().ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        SetEventState(L10NLookup("GOLDEN_IDOL.pages.BOULDER.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(OutrunOption),
                "GOLDEN_IDOL.pages.BOULDER.options.OUTRUN",
                HoverTipFactory.FromCard(ModelDb.Card<Injury>())),
            new EventOption(this, new Func<Task>(SmashOption),
                "GOLDEN_IDOL.pages.BOULDER.options.SMASH",
                Array.Empty<IHoverTip>()).ThatDoesDamage(DynamicVars["Damage"].BaseValue),
            new EventOption(this, new Func<Task>(CrawlOption),
                "GOLDEN_IDOL.pages.BOULDER.options.CRAWL",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task OutrunOption()
    {
        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
        SetEventFinished(L10NLookup("GOLDEN_IDOL.pages.OUTRUN.description"));
    }

    private async Task SmashOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(L10NLookup("GOLDEN_IDOL.pages.SMASH.description"));
    }

    private async Task CrawlOption()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue,
            false);
        SetEventFinished(L10NLookup("GOLDEN_IDOL.pages.CRAWL.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("GOLDEN_IDOL.pages.LEAVE.description"));
    }
}