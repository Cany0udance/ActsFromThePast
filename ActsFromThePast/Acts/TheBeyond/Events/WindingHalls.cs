using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class WindingHalls : CustomEventModel
{
    private const decimal HpLossPercent = 0.18M;
    private const decimal HealPercent = 0.20M;
    private const decimal MaxHpLossPercent = 0.05M;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", 0),
        new IntVar("HealAmt", 0),
        new IntVar("MaxHpLoss", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["HpLoss"].BaseValue =
            Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars["HealAmt"].BaseValue =
            Math.Round(Owner.Creature.MaxHp * HealPercent);
        DynamicVars["MaxHpLoss"].BaseValue =
            Math.Round(Owner.Creature.MaxHp * MaxHpLossPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "winding_halls");
    }

    private Task Continue()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            SetEventState(PageDescription("CHOICE"), new[]
            {
                Option(Madness, "CHOICE_REBALANCED",
                    HoverTipFactory.FromCardWithCardHoverTips<Madness>().ToArray()),
                Option(Retreat, "CHOICE")
            });
        }
        else
        {
            SetEventState(PageDescription("CHOICE"), new[]
            {
                Option(Madness, "CHOICE",
                        HoverTipFactory.FromCardWithCardHoverTips<Madness>().ToArray())
                    .ThatDoesDamage(DynamicVars["HpLoss"].BaseValue),
                Option(Writhe, "CHOICE",
                    HoverTipFactory.FromCardWithCardHoverTips<Writhe>().ToArray()),
                Option(Retreat, "CHOICE")
            });
        }
        return Task.CompletedTask;
    }

    private async Task Madness()
    {
        int count;

        if (ActsFromThePastConfig.RebalancedMode)
        {
            count = 1;
        }
        else
        {
            count = 2;
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["HpLoss"].BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }

        ModAudio.Play("general", "attack_magic_slow_1");
        for (int i = 0; i < count; i++)
        {
            var card = Owner.RunState.CreateCard(ModelDb.Card<Madness>(), Owner);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd(new[] { result }, 2f);
        }
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("MADNESS"));
    }

    private async Task Writhe()
    {
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars["HealAmt"].BaseValue);
        var card = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { result }, 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("WRITHE"));
    }

    private async Task Retreat()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue,
            false);
        SetEventFinished(PageDescription("RETREAT"));
    }
}