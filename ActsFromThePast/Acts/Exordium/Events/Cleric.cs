using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Cleric : EventModel
{
    private const int HealCost = 35;
    private const int PurifyCost = 75;
    private const decimal HealPercent = 0.25M;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new HealVar(0M),
                new IntVar("HealCost", HealCost),
                new IntVar("PurifyCost", PurifyCost)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars.Heal.BaseValue = Math.Floor(Owner.Creature.MaxHp * HealPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Gold >= HealCost)
        {
            options.Add(new EventOption(this, new Func<Task>(HealOption),
                "CLERIC.pages.INITIAL.options.HEAL"));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "CLERIC.pages.INITIAL.options.HEAL_LOCKED"));
        }

        bool canPurify = Owner.Gold >= PurifyCost && 
                         Owner.Deck.Cards.Any<CardModel>(c => c.IsRemovable);
        if (canPurify)
        {
            options.Add(new EventOption(this, new Func<Task>(PurifyOption),
                "CLERIC.pages.INITIAL.options.PURIFY"));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "CLERIC.pages.INITIAL.options.PURIFY_LOCKED"));
        }

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "CLERIC.pages.INITIAL.options.LEAVE"));

        return options;
    }

    private async Task HealOption()
    {
        await PlayerCmd.LoseGold(HealCost, Owner, GoldLossType.Spent);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(L10NLookup("CLERIC.pages.HEAL.description"));
    }

    private async Task PurifyOption()
    {
        await PlayerCmd.LoseGold(PurifyCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList<CardModel>());
        SetEventFinished(L10NLookup("CLERIC.pages.PURIFY.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("CLERIC.pages.LEAVE.description"));
    }
    
    public override bool IsAllowed(RunState runState)
    {
        return runState.Players.All<Player>(p => p.Gold >= HealCost);
    }
}