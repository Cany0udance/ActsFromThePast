using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Sssserpent : CustomEventModel
{
    private static int GoldReward => ActsFromThePastConfig.RebalancedMode ? 250 : 150;

    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(GoldReward)
    };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Agree, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
                Option(DisagreeRebalanced, "INITIAL_REBALANCED")
            };
        }

        return new[]
        {
            Option(Agree, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
            Option(Disagree)
        };
    }

    private Task Agree()
    {
        SetEventState(PageDescription("AGREE"), new[]
        {
            Option(TakeGold, "AGREE")
        });
        return Task.CompletedTask;
    }

    private async Task TakeGold()
    {
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("TAKE_GOLD"));
    }

    private Task Disagree()
    {
        SetEventFinished(PageDescription("DISAGREE"));
        return Task.CompletedTask;
    }
    
    
    private async Task DisagreeRebalanced()
    {
        var options = new CardCreationOptions(
            new[] { Owner.Character.CardPool },
            CardCreationSource.Other,
            CardRarityOddsType.Uniform,
            c => c.Rarity == CardRarity.Rare
        ).WithFlags(CardCreationFlags.NoUpgradeRoll);

        var list = CardFactory.CreateForReward(Owner, 1, options)
            .Select(r => r.Card)
            .ToList();

        if (list.Count > 0)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(list[0], PileType.Deck));

        SetEventFinished(PageDescription("DISAGREE_REBALANCED"));
    }
}