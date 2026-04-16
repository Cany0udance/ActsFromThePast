using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class Falling : CustomEventModel
{
    private CardModel? _attackCard;
    private CardModel? _skillCard;
    private CardModel? _powerCard;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new StringVar("SkillCard"),
        new StringVar("PowerCard"),
        new StringVar("AttackCard")
    };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "falling");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        SetCards();
        return new[] { Option(Continue) };
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

    private Task Continue()
    {
        var options = new List<EventOption>();

        if (_skillCard != null)
            options.Add(Option(Skill, "CHOICE", HoverTipFactory.FromCard(_skillCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.SKILL_LOCKED",
                Array.Empty<IHoverTip>()));

        if (_powerCard != null)
            options.Add(Option(Power, "CHOICE", HoverTipFactory.FromCard(_powerCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.POWER_LOCKED",
                Array.Empty<IHoverTip>()));

        if (_attackCard != null)
            options.Add(Option(Attack, "CHOICE", HoverTipFactory.FromCard(_attackCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.ATTACK_LOCKED",
                Array.Empty<IHoverTip>()));

        SetEventState(PageDescription("CHOICE"), options);
        return Task.CompletedTask;
    }

    private async Task Skill()
    {
        await CardPileCmd.RemoveFromDeck(_skillCard);
        SetEventFinished(PageDescription("SKILL"));
    }

    private async Task Power()
    {
        await CardPileCmd.RemoveFromDeck(_powerCard);
        SetEventFinished(PageDescription("POWER"));
    }

    private async Task Attack()
    {
        await CardPileCmd.RemoveFromDeck(_attackCard);
        SetEventFinished(PageDescription("ATTACK"));
    }
}