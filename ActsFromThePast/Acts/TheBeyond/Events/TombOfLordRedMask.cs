using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class TombOfLordRedMask : CustomEventModel
{
    private const int GoldAmount = 222;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(GoldAmount),
        new IntVar("PlayerGold", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["PlayerGold"].BaseValue = Owner.Gold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is RedMask))
        {
            options.Add(Option(WearMask));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.WEAR_MASK_LOCKED",
                Array.Empty<IHoverTip>()));
            options.Add(Option(PayRespects, "INITIAL",
                HoverTipFactory.FromRelic(ModelDb.Relic<RedMask>()).ToArray()));
        }

        options.Add(Option(Leave));
        return options;
    }

    private async Task WearMask()
    {
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(PageDescription("WEAR_MASK"));
    }

    private async Task PayRespects()
    {
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
        {
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Spent);
        }
        var redMask = ModelDb.Relic<RedMask>().ToMutable();
        await RelicCmd.Obtain(redMask, Owner);
        SetEventFinished(PageDescription("PAY_RESPECTS"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}