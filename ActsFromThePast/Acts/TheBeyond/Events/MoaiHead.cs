using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MoaiHead : CustomEventModel
{
    private const decimal HpLossPercent = 0.125M;
    private const int GoldAmount = 333;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", 0),
        new GoldVar(GoldAmount)
    };

    public override void CalculateVars()
    {
        var hpLoss = (int)Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars["HpLoss"].BaseValue = hpLoss;
    }

    private bool HasVisitedExordium(IRunState runState)
    {
        for (int i = 0; i < runState.CurrentActIndex; i++)
        {
            if (runState.Acts[i] is ExordiumAct)
                return true;
        }
        return false;
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (HasVisitedExordium(runState))
            return true;
        return runState.Players.Any(p =>
            (decimal)p.Creature.CurrentHp / p.Creature.MaxHp <= 0.5M);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Pray)
        };

        if (Owner.Relics.Any(r => r is Relics.GoldenIdol))
            options.Add(Option(OfferIdol));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.OFFER_IDOL_LOCKED",
                Array.Empty<IHoverTip>()));

        options.Add(Option(Leave));
        return options;
    }

    private async Task Pray()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            false);
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task OfferIdol()
    {
        var goldenIdol = Owner.Relics.First(r => r is Relics.GoldenIdol);
        await RelicCmd.Remove(goldenIdol);
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(PageDescription("OFFER_IDOL"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}