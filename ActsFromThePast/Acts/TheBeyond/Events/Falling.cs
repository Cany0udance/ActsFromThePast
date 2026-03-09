using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class Falling : EventModel
{
    private CardModel? _attackCard;
    private CardModel? _skillCard;
    private CardModel? _powerCard;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new StringVar("SkillCard"),
                new StringVar("PowerCard"),
                new StringVar("AttackCard")
            };
        }
    }
    
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "falling");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        SetCards();
        return new[]
        {
            new EventOption(this, IntroOption,
                "FALLING.pages.INITIAL.options.CONTINUE")
        };
    }

    private void SetCards()
    {
        var deck = Owner.Deck.Cards;

        var skills = deck.Where(c => c.Type == CardType.Skill && c.IsRemovable).ToList();
        var powers = deck.Where(c => c.Type == CardType.Power && c.IsRemovable).ToList();
        var attacks = deck.Where(c => c.Type == CardType.Attack && c.IsRemovable).ToList();

        if (skills.Count > 0)
        {
            _skillCard = Rng.NextItem(skills);
            ((StringVar)DynamicVars["SkillCard"]).StringValue = _skillCard.Title;
        }
        if (powers.Count > 0)
        {
            _powerCard = Rng.NextItem(powers);
            ((StringVar)DynamicVars["PowerCard"]).StringValue = _powerCard.Title;
        }
        if (attacks.Count > 0)
        {
            _attackCard = Rng.NextItem(attacks);
            ((StringVar)DynamicVars["AttackCard"]).StringValue = _attackCard.Title;
        }
    }

    private Task IntroOption()
    {
        var options = new List<EventOption>();

        if (_skillCard != null)
        {
            options.Add(new EventOption(this, RemoveSkill,
                "FALLING.pages.CHOICE.options.SKILL",
                new IHoverTip[] { HoverTipFactory.FromCard(_skillCard) }));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "FALLING.pages.CHOICE.options.SKILL_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        if (_powerCard != null)
        {
            options.Add(new EventOption(this, RemovePower,
                "FALLING.pages.CHOICE.options.POWER",
                new IHoverTip[] { HoverTipFactory.FromCard(_powerCard) }));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "FALLING.pages.CHOICE.options.POWER_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        if (_attackCard != null)
        {
            options.Add(new EventOption(this, RemoveAttack,
                "FALLING.pages.CHOICE.options.ATTACK",
                new IHoverTip[] { HoverTipFactory.FromCard(_attackCard) }));
        }
        else
        {
            options.Add(new EventOption(this, null,
                "FALLING.pages.CHOICE.options.ATTACK_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        SetEventState(
            L10NLookup("FALLING.pages.CHOICE.description"),
            options
        );
        return Task.CompletedTask;
    }

    private async Task RemoveSkill()
    {
        await CardPileCmd.RemoveFromDeck(_skillCard);
        SetEventFinished(L10NLookup("FALLING.pages.SKILL.description"));
    }

    private async Task RemovePower()
    {
        await CardPileCmd.RemoveFromDeck(_powerCard);
        SetEventFinished(L10NLookup("FALLING.pages.POWER.description"));
    }

    private async Task RemoveAttack()
    {
        await CardPileCmd.RemoveFromDeck(_attackCard);
        SetEventFinished(L10NLookup("FALLING.pages.ATTACK.description"));
    }
}