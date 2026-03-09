using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WingStatue : EventModel
{
    private const int Damage = 7;
    private const int RequiredDamage = 10;
    private const int MinGold = 50;
    private const int MaxGold = 80;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Damage", Damage),
                new GoldVar(0),
                new IntVar("RequiredDamage", RequiredDamage)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = MinGold + Rng.NextInt(MaxGold - MinGold + 1);
    }

    private bool CanAttack()
    {
        return Owner.Deck.Cards.Any(c => 
            c.Type == CardType.Attack && 
            c.DynamicVars.ContainsKey("Damage") && 
            c.DynamicVars.Damage.BaseValue >= RequiredDamage);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        options.Add(new EventOption(this, new Func<Task>(AgreeOption),
            "WING_STATUE.pages.INITIAL.options.AGREE",
            Array.Empty<IHoverTip>()).ThatDoesDamage(Damage));

        if (CanAttack())
        {
            options.Add(new EventOption(this, new Func<Task>(AttackOption),
                "WING_STATUE.pages.INITIAL.options.ATTACK",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "WING_STATUE.pages.INITIAL.options.ATTACK_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "WING_STATUE.pages.INITIAL.options.LEAVE",
            Array.Empty<IHoverTip>()));

        return options;
    }

    private async Task AgreeOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            Damage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());

        SetEventFinished(L10NLookup("WING_STATUE.pages.AGREE.description"));
    }

    private async Task AttackOption()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(L10NLookup("WING_STATUE.pages.ATTACK.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("WING_STATUE.pages.LEAVE.description"));
    }
}