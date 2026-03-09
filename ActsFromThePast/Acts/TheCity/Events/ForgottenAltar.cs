using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class ForgottenAltar : EventModel
{
    private const float HpLossPercent = 0.35f;
    private const int MaxHpGain = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("HpLoss", 0),
                new IntVar("MaxHpGain", MaxHpGain)
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
        // Check if any previously visited act was Exordium
        for (int i = 0; i < runState.CurrentActIndex; i++)
        {
            if (runState.Acts[i] is ExordiumAct)
                return true;
        }
        return false;
    }

    public override bool IsAllowed(RunState runState)
    {
        return HasVisitedExordium(runState);
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "forgotten_altar");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is GoldenIdolOriginal))
        {
            options.Add(new EventOption(this, new Func<Task>(OfferIdolOption),
                "FORGOTTEN_ALTAR.pages.INITIAL.options.OFFER_IDOL",
                HoverTipFactory.FromRelic(ModelDb.Relic<BloodyIdol>())));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "FORGOTTEN_ALTAR.pages.INITIAL.options.OFFER_IDOL_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        var netDamage = DynamicVars["HpLoss"].BaseValue - MaxHpGain;
        options.Add(new EventOption(this, new Func<Task>(SacrificeOption),
                "FORGOTTEN_ALTAR.pages.INITIAL.options.SACRIFICE",
                Array.Empty<IHoverTip>())
            .ThatDoesDamage(netDamage));

        options.Add(new EventOption(this, new Func<Task>(DesecrateOption),
            "FORGOTTEN_ALTAR.pages.INITIAL.options.DESECRATE",
            HoverTipFactory.FromCard(ModelDb.Card<Decay>())));

        return options;
    }

    private async Task OfferIdolOption()
    {
        SfxCmd.Play("event:/sfx/heal_1");
    
        var goldenIdol = Owner.Relics.First(r => r is GoldenIdolOriginal);
        var bloodyIdol = ModelDb.Relic<BloodyIdol>().ToMutable();
        await RelicCmd.Replace(goldenIdol, bloodyIdol);

        SetEventFinished(L10NLookup("FORGOTTEN_ALTAR.pages.OFFER_IDOL.description"));
    }

    private async Task SacrificeOption()
    {
        SfxCmd.Play("event:/sfx/heal_3");
        
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(L10NLookup("FORGOTTEN_ALTAR.pages.SACRIFICE.description"));
    }

    private async Task DesecrateOption()
    {
        SfxCmd.Play("event:/sfx/blunt_heavy");
        
        var decay = Owner.RunState.CreateCard(ModelDb.Card<Decay>(), Owner);
        var addResult = await CardPileCmd.Add(decay, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
        await Cmd.Wait(0.75f);

        SetEventFinished(L10NLookup("FORGOTTEN_ALTAR.pages.DESECRATE.description"));
    }
}