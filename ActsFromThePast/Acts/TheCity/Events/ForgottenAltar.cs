using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
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
using GoldenIdol = ActsFromThePast.Relics.GoldenIdol;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class ForgottenAltar : CustomEventModel
{
    private const float HpLossPercent = 0.35f;
    private const int MaxHpGain = 5;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", 0),
        new IntVar("MaxHpGain", MaxHpGain)
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
        return HasVisitedExordium(runState);
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "forgotten_altar");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is GoldenIdol))
            options.Add(Option(OfferIdol, "INITIAL",
                HoverTipFactory.FromRelic(ModelDb.Relic<BloodyIdol>()).ToArray()));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.OFFER_IDOL_LOCKED",
                Array.Empty<IHoverTip>()));

        var netDamage = DynamicVars["HpLoss"].BaseValue - MaxHpGain;
        options.Add(Option(Sacrifice).ThatDoesDamage(netDamage));

        options.Add(Option(Desecrate, "INITIAL",
            HoverTipFactory.FromCard(ModelDb.Card<Decay>())));

        return options;
    }

    private async Task OfferIdol()
    {
        SfxCmd.Play("event:/sfx/heal_1");

        var goldenIdol = Owner.Relics.First(r => r is GoldenIdol);
        var bloodyIdol = ModelDb.Relic<BloodyIdol>().ToMutable();
        await RelicCmd.Replace(goldenIdol, bloodyIdol);
        SetEventFinished(PageDescription("OFFER_IDOL"));
    }

    private async Task Sacrifice()
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
        SetEventFinished(PageDescription("SACRIFICE"));
    }

    private async Task Desecrate()
    {
        SfxCmd.Play("event:/sfx/blunt_heavy");

        var decay = Owner.RunState.CreateCard(ModelDb.Card<Decay>(), Owner);
        var addResult = await CardPileCmd.Add(decay, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("DESECRATE"));
    }
}