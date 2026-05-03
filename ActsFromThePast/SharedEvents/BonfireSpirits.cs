using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class BonfireSpirits : CustomEventModel
{
    private const int CommonHeal = 5;
    private const int RareMaxHpGain = 10;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }
    
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "goop");
    }

    private Task Continue()
    {
        SetEventState(PageDescription("APPROACH"), new[]
        {
            Option(Offer, "APPROACH")
        });
        return Task.CompletedTask;
    }

    private async Task Offer()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).FirstOrDefault();

        if (card == null)
        {
            SetEventFinished(PageDescription("NOTHING"));
            return;
        }

        var rarity = card.Rarity;
        var isCurse = card.Type == CardType.Curse;

        await CardPileCmd.RemoveFromDeck(new List<CardModel> { card });

        if (isCurse)
        {
            await RelicCmd.Obtain(ModelDb.Relic<SpiritPoop>().ToMutable(), Owner);
            SetEventFinished(PageDescription("OFFER_CURSE"));
        }
        else if (rarity == CardRarity.Basic)
        {
            SetEventFinished(PageDescription("OFFER_BASIC"));
        }
        else if (rarity == CardRarity.Common)
        {
            await CreatureCmd.Heal(Owner.Creature, CommonHeal);
            SetEventFinished(PageDescription("OFFER_COMMON"));
        }
        else if (rarity == CardRarity.Uncommon || rarity == CardRarity.Quest)
        {
            await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
            SetEventFinished(PageDescription("OFFER_UNCOMMON"));
        }
        else if (rarity == CardRarity.Rare || rarity == CardRarity.Ancient)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, RareMaxHpGain);
            await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
            SetEventFinished(PageDescription("OFFER_RARE"));
        }
        else
        {
            await CreatureCmd.Heal(Owner.Creature, CommonHeal);
            SetEventFinished(PageDescription("OFFER_COMMON"));
        }
    }
}