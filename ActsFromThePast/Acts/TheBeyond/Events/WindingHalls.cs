using ActsFromThePast.Cards;
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

public sealed class WindingHalls : EventModel
{
    private const decimal HpLossPercent = 0.125M;
    private const decimal HealPercent = 0.25M;
    private const decimal MaxHpLossPercent = 0.05M;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("HpLoss", 0),
                new IntVar("HealAmt", 0),
                new IntVar("MaxHpLoss", 0)
            };
        }
    }

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
        return new[]
        {
            new EventOption(this, IntroOption,
                "WINDING_HALLS.pages.INITIAL.options.CONTINUE")
        };
    }
    
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "winding_halls");
    }

    private Task IntroOption()
    {
        SetEventState(
            L10NLookup("WINDING_HALLS.pages.CHOICE.description"),
            new[]
            {
                new EventOption(this, MadnessOption,
                    "WINDING_HALLS.pages.CHOICE.options.MADNESS",
                    HoverTipFactory.FromCardWithCardHoverTips<Madness>())
                    .ThatDoesDamage(DynamicVars["HpLoss"].BaseValue),
                new EventOption(this, WritheOption,
                    "WINDING_HALLS.pages.CHOICE.options.WRITHE",
                    HoverTipFactory.FromCardWithCardHoverTips<Writhe>()),
                new EventOption(this, RetreatOption,
                    "WINDING_HALLS.pages.CHOICE.options.RETREAT")
            }
        );
        return Task.CompletedTask;
    }

    private async Task MadnessOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        
        ModAudio.Play("general", "attack_magic_slow_1");

        for (int i = 0; i < 2; i++)
        {
            var card = Owner.RunState.CreateCard(ModelDb.Card<Madness>(), Owner);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd(new[] { result }, 2f);
        }
        await Cmd.Wait(0.75f);

        SetEventFinished(L10NLookup("WINDING_HALLS.pages.MADNESS.description"));
    }

    private async Task WritheOption()
    {
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars["HealAmt"].BaseValue);

        var card = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { result }, 2f);
        await Cmd.Wait(0.75f);

        SetEventFinished(L10NLookup("WINDING_HALLS.pages.WRITHE.description"));
    }

    private async Task RetreatOption()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue,
            false);

        SetEventFinished(L10NLookup("WINDING_HALLS.pages.RETREAT.description"));
    }
}