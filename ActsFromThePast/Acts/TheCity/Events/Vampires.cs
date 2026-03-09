using ActsFromThePast.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Vampires : EventModel
{
    private const decimal HpDrainPercent = 0.3M;
    private const int BiteCount = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("MaxHpLoss", 0)
            };
        }
    }

    public override void CalculateVars()
    {
        var maxHpLoss = (int)Math.Ceiling(Owner.Creature.MaxHp * HpDrainPercent);
        if (maxHpLoss >= Owner.Creature.MaxHp)
            maxHpLoss = Owner.Creature.MaxHp - 1;
        DynamicVars["MaxHpLoss"].BaseValue = maxHpLoss;
    }

    private bool HasBloodVial()
    {
        return Owner.Relics.Any(r => r is BloodVial);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            new EventOption(this, new Func<Task>(AcceptOption),
                "VAMPIRES.pages.INITIAL.options.ACCEPT",
                HoverTipFactory.FromCard(ModelDb.Card<Bite>()))
        };

        if (HasBloodVial())
        {
            options.Add(new EventOption(this, new Func<Task>(VialOption),
                "VAMPIRES.pages.INITIAL.options.VIAL",
                HoverTipFactory.FromCard(ModelDb.Card<Bite>())));
        }

        options.Add(new EventOption(this, new Func<Task>(LeaveOption),
            "VAMPIRES.pages.INITIAL.options.LEAVE",
            Array.Empty<IHoverTip>()));

        return options;
    }

    private async Task ReplaceStrikesWithBites()
    {
        var strikesToRemove = Owner.Deck.Cards
            .Where(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike))
            .ToList();

        foreach (var strike in strikesToRemove)
        {
            await CardPileCmd.RemoveFromDeck(new[] { strike });
        }

        var biteResults = new List<CardPileAddResult>();
        for (var i = 0; i < BiteCount; i++)
        {
            var bite = Owner.RunState.CreateCard(ModelDb.Card<Bite>(), Owner);
            biteResults.Add(await CardPileCmd.Add(bite, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)biteResults, 2f);
        await Cmd.Wait(0.75f);
    }

    private async Task AcceptOption()
    {
        ModAudio.Play("general", "bite");
        var maxHpLoss = (int)DynamicVars["MaxHpLoss"].BaseValue;
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            maxHpLoss,
            false);
        await ReplaceStrikesWithBites();
        SetEventFinished(L10NLookup("VAMPIRES.pages.ACCEPT.description"));
    }

    private async Task VialOption()
    {
        ModAudio.Play("general", "bite");
        var vial = Owner.Relics.First(r => r is BloodVial);
        await RelicCmd.Remove(vial);
        await ReplaceStrikesWithBites();
        SetEventFinished(L10NLookup("VAMPIRES.pages.VIAL.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("VAMPIRES.pages.LEAVE.description"));
    }
}