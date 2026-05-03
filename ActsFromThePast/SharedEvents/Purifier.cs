using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class Purifier : CustomEventModel
{
    private const decimal HpLossPercent = 0.15M;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["HpLoss"].BaseValue = (int)Math.Round(Owner.Creature.MaxHp * HpLossPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Pray),
                Option(Kneel, "INITIAL_REBALANCED")
                    .ThatDecreasesMaxHp(DynamicVars["HpLoss"].BaseValue)
            };
        }
        return new[]
        {
            Option(Pray),
            Option(Leave)
        };
    }

    private async Task Pray()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());

        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task Kneel()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            false);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}