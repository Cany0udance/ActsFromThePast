using BaseLib.Abstracts;
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

public sealed class Cleric : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    private const int HealCost = 35;
    private const int PurifyCost = 75;
    private const decimal HealPercent = 0.25M;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HealVar(0M),
        new IntVar("HealCost", HealCost),
        new IntVar("PurifyCost", PurifyCost)
    };

    public override void CalculateVars()
    {
        DynamicVars.Heal.BaseValue = Math.Floor(Owner.Creature.MaxHp * HealPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (Owner.Gold >= HealCost)
            options.Add(Option(Heal));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.HEAL_LOCKED",
                Array.Empty<IHoverTip>()));

        bool canPurify = Owner.Gold >= PurifyCost &&
                         Owner.Deck.Cards.Any<CardModel>(c => c.IsRemovable);

        if (canPurify)
            options.Add(Option(Purify));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.PURIFY_LOCKED",
                Array.Empty<IHoverTip>()));

        options.Add(Option(Leave));
        return options;
    }

    private async Task Heal()
    {
        await PlayerCmd.LoseGold(HealCost, Owner, GoldLossType.Spent);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(PageDescription("HEAL"));
    }

    private async Task Purify()
    {
        await PlayerCmd.LoseGold(PurifyCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList<CardModel>());
        SetEventFinished(PageDescription("PURIFY"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All<Player>(p => p.Gold >= HealCost);
    }
}