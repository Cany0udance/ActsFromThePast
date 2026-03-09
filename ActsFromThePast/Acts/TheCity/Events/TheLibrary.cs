using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheLibrary : EventModel
{
    private const int CardChoiceCount = 20;
    private const decimal HealPercent = 0.2M;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new HealVar(0M),
                new IntVar("CardChoiceCount", CardChoiceCount)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars.Heal.BaseValue = Math.Round(Owner.Creature.MaxHp * HealPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(ReadOption),
                "THE_LIBRARY.pages.INITIAL.options.READ",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(SleepOption),
                "THE_LIBRARY.pages.INITIAL.options.SLEEP",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task ReadOption()
    {
        var cardResults = CardFactory.CreateForReward(
                Owner, 
                CardChoiceCount, 
                CardCreationOptions.ForNonCombatWithDefaultOdds(Owner.Character.CardPool.AllCards))
            .ToList();
        var prefs = new CardSelectorPrefs(L10NLookup("THE_LIBRARY.pages.READ.selectionScreenPrompt"), 1)
        {
            Cancelable = false
        };
        var selectedCard = (await CardSelectCmd.FromSimpleGridForRewards(
            new BlockingPlayerChoiceContext(), 
            cardResults, 
            Owner, 
            prefs)).FirstOrDefault();
        if (selectedCard != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selectedCard, PileType.Deck));
        }
        var bookText = GetRandomBookText();
        SetEventFinished(bookText);
    }

    private async Task SleepOption()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(L10NLookup("THE_LIBRARY.pages.SLEEP.description"));
    }

    private LocString GetRandomBookText()
    {
        var bookIndex = Rng.NextInt(3);
        return bookIndex switch
        {
            0 => L10NLookup("THE_LIBRARY.pages.READ.description_1"),
            1 => L10NLookup("THE_LIBRARY.pages.READ.description_2"),
            _ => L10NLookup("THE_LIBRARY.pages.READ.description_3")
        };
    }
}