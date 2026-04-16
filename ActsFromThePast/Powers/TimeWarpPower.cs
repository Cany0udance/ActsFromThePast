using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Powers;

public sealed class TimeWarpPower : CustomPowerModel
{
    private const int StrengthAmount = 2;
    private const int CountdownAmount = 12;
    private const string _cardCountKey = "CardCount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;
    public override int DisplayAmount => DynamicVars[_cardCountKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new[] { new DynamicVar(_cardCountKey, 0M) };
        }
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        DynamicVars[_cardCountKey].BaseValue++;
        InvokeDisplayAmountChanged();

        if (DynamicVars[_cardCountKey].IntValue >= CountdownAmount)
        {
            DynamicVars[_cardCountKey].BaseValue = 0M;
            InvokeDisplayAmountChanged();

            Flash();
            ModAudio.Play("time_eater", "time_warp");
            BorderFlashEffect.PlayGold();

            var effect = TimeWarpTurnEndEffect.Create();
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(effect);

            PlayerCmd.EndTurn(cardPlay.Card.Owner, false);

            foreach (var enemy in Owner.CombatState.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<StrengthPower>(enemy, StrengthAmount, Owner, null);
        }
    }
}