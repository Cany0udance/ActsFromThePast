using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.Exordium.Events;
// 600 from left, 332 from bottom. 800x800, 30 stroke 50% opacity, big image complete gaussian blur 2750x2750
public sealed class BigFish : EventModel
{
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
            new EventOption(this, new Func<Task>(BananaOption),
                "BIG_FISH.pages.INITIAL.options.BANANA",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(DonutOption),
                "BIG_FISH.pages.INITIAL.options.DONUT",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(BoxOption),
                "BIG_FISH.pages.INITIAL.options.BOX",
                HoverTipFactory.FromCard(ModelDb.Card<Regret>()))
        };
    }
    
    private async Task BananaOption()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(L10NLookup("BIG_FISH.pages.BANANA.description"));
    }
    
    private async Task DonutOption()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        SetEventFinished(L10NLookup("BIG_FISH.pages.DONUT.description"));
    }
    
    private async Task BoxOption()
    {
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(L10NLookup("BIG_FISH.pages.BOX.description"));
    }
}