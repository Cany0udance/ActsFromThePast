using ActsFromThePast.Afflictions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class EntangledPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ModAudio.Play("general", "entangle");
        foreach (var card in Owner.Player.PlayerCombatState.AllCards.Where(c => c.Type == CardType.Attack))
        {
            await CardCmd.Afflict<EntangledOriginal>(card, 1m);
        }
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner.Player || card.Affliction != null || card.Type != CardType.Attack)
            return;

        await CardCmd.Afflict<EntangledOriginal>(card, 1m);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        Flash();
        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        foreach (var card in oldOwner.Player.PlayerCombatState.AllCards.Where(c => c.Affliction is EntangledOriginal))
        {
            CardCmd.ClearAffliction(card);
        }

        return Task.CompletedTask;
    }
}