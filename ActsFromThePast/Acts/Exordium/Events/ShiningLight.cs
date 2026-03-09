using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ShiningLight : EventModel
{
    private const decimal HpLossPercent = 0.30M;
    private const int UpgradeCount = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Damage", 0),
                new CardsVar(UpgradeCount)
            };
        }
    }

    public override void CalculateVars()
    {
        DynamicVars["Damage"].BaseValue = Math.Floor(Owner.Creature.MaxHp * HpLossPercent);
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "shining_light");
    }

    private bool HasUpgradableCards()
    {
        return PileType.Deck.GetPile(Owner).Cards.Any(c => c != null && c.IsUpgradable);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (HasUpgradableCards())
        {
            options.Add(new EventOption(this, new Func<Task>(EnterLightOption),
                "SHINING_LIGHT.pages.INITIAL.options.ENTER",
                Array.Empty<IHoverTip>()).ThatDoesDamage(DynamicVars["Damage"].BaseValue));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "SHINING_LIGHT.pages.INITIAL.options.ENTER_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "SHINING_LIGHT.pages.INITIAL.options.LEAVE",
            Array.Empty<IHoverTip>()));

        return options;
    }

    private async Task EnterLightOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        var upgradableCards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c != null && c.IsUpgradable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(DynamicVars.Cards.IntValue);

        foreach (var card in upgradableCards)
        {
            CardCmd.Upgrade(card);
        }

        SetEventFinished(L10NLookup("SHINING_LIGHT.pages.ENTER.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("SHINING_LIGHT.pages.LEAVE.description"));
    }
}