using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.Exordium.Events;
// 600 from left, 332 from bottom. 800x800, 30 stroke 50% opacity, big image complete gaussian blur 2750x2750
public sealed class BigFish : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };
    
    private const int MaxHpGain = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new HealVar(0M),
                new IntVar("MaxHpGain", MaxHpGain)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars.Heal.BaseValue = Owner.Creature.MaxHp / 3M;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            Option(Banana),
            Option(Donut),
            Option(Box, new[] { HoverTipFactory.FromCard(ModelDb.Card<Regret>()) })
        };
    }

    private async Task Banana()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(PageDescription("BANANA"));
    }

    private async Task Donut()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        SetEventFinished(PageDescription("DONUT"));
    }

    private async Task Box()
    {
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("BOX"));
    }
}