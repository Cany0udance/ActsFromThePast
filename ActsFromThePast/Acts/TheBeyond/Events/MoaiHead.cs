using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MoaiHead : EventModel
{
    private const decimal HpLossPercent = 0.125M;
    private const int GoldAmount = 333;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("HpLoss", 0),
                new GoldVar(GoldAmount)
            };
        }
    }

    public override void CalculateVars()
    {
        var hpLoss = (int)Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars["HpLoss"].BaseValue = hpLoss;
    }
    
    private bool HasVisitedExordium(RunState runState)
    {
        for (int i = 0; i < runState.CurrentActIndex; i++)
        {
            if (runState.Acts[i] is ExordiumAct)
                return true;
        }
        return false;
    }

    public override bool IsAllowed(RunState runState)
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
            new EventOption(this, PrayOption,
                "MOAI_HEAD.pages.INITIAL.options.PRAY")
        };

        if (Owner.Relics.Any(r => r is GoldenIdolOriginal))
        {
            options.Add(new EventOption(this, OfferIdolOption,
                "MOAI_HEAD.pages.INITIAL.options.OFFER_IDOL"));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "MOAI_HEAD.pages.INITIAL.options.OFFER_IDOL_LOCKED"));
        }

        options.Add(new EventOption(this, LeaveOption,
            "MOAI_HEAD.pages.INITIAL.options.LEAVE"));

        return options;
    }

    private async Task PrayOption()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            false);
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        SetEventFinished(L10NLookup("MOAI_HEAD.pages.PRAY.description"));
    }

    private async Task OfferIdolOption()
    {
        var goldenIdol = Owner.Relics.First(r => r is GoldenIdolOriginal);
        await RelicCmd.Remove(goldenIdol);
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(L10NLookup("MOAI_HEAD.pages.OFFER_IDOL.description"));
    }

    private Task LeaveOption()
    {
        SetEventFinished(L10NLookup("MOAI_HEAD.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}