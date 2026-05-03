using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class TheDivineFountain : CustomEventModel
{
    private const int MaxHpPerCurse = 3;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("MaxHpGain", 0)
    };

    public override void CalculateVars()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            var curseCount = Owner.Deck.Cards.Count(c => c.Type == CardType.Curse);
            DynamicVars["MaxHpGain"].BaseValue = curseCount * MaxHpPerCurse;
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c =>
                c.Type == CardType.Curse && c.IsRemovable && c is not Guilty));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Drink),
                Option(Bathe, "INITIAL_REBALANCED")
            };
        }
        return new[]
        {
            Option(Drink),
            Option(Leave)
        };
    }

    private async Task Drink()
    {
        var curses = Owner.Deck.Cards
            .Where(c => c.Type == CardType.Curse && c.IsRemovable)
            .ToList();

        await CardPileCmd.RemoveFromDeck(curses);

        SetEventFinished(PageDescription("DRINK"));
    }

    private async Task Bathe()
    {
        var curseCount = Owner.Deck.Cards.Count(c => c.Type == CardType.Curse);
        var maxHpGain = curseCount * MaxHpPerCurse;

        if (maxHpGain > 0)
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);

        SetEventFinished(PageDescription("BATHE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}