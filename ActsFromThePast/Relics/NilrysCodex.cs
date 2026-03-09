using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Relics;

public sealed class NilrysCodex : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        if (CombatManager.Instance == null || !CombatManager.Instance.IsInProgress)
            return;

        Flash();

        var cardChoices = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        var selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            cardChoices,
            Owner,
            true);

        if (selectedCard == null)
            return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Draw, true));
    }
}