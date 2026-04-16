using ActsFromThePast.Relics;
using BaseLib.Abstracts;
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

public sealed class GoldenIdol : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    private const decimal HpLossPercent = 0.35M;
    private const decimal MaxHpLossPercent = 0.10M;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Damage", 0),
        new IntVar("MaxHpLoss", 0)
    };

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
        return new[]
        {
            Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
            Option(Leave)
        };
    }

    private async Task Take()
    {
        var relic = ModelDb.Relic<Relics.GoldenIdol>().ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventState(PageDescription("BOULDER"), new[]
        {
            Option(Outrun, "BOULDER", HoverTipFactory.FromCard(ModelDb.Card<Injury>())),
            Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
            Option(Crawl, "BOULDER")
        });
    }

    private async Task Outrun()
    {
        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
        SetEventFinished(PageDescription("OUTRUN"));
    }

    private async Task Smash()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(PageDescription("SMASH"));
    }

    private async Task Crawl()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue,
            false);
        SetEventFinished(PageDescription("CRAWL"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}