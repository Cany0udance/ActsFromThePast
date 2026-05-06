using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class WheelOfChange : CustomEventModel
{
    private const decimal HpLossPercent = 0.15M;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("HpLoss", 0),
        new IntVar("GoldAmount", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["HpLoss"].BaseValue =
            (int)(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars["GoldAmount"].BaseValue = GetGoldAmount();
    }

    private int GetGoldAmount()
    {
        var actIndex = Owner.RunState.CurrentActIndex;
        return actIndex switch
        {
            0 => 100,
            1 => 200,
            _ => 300
        };
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Play) };
    }

    private async Task Play()
    {
        var result = Rng.NextInt(6);
        var minigame = new WheelSpinMinigame(Owner, result, Owner.RunState.CurrentActIndex);
        await minigame.PlayMinigame();
        ShowResult(result);
    }

    private void ShowResult(int result)
    {
        var (pageKey, optionKey) = result switch
        {
            0 => ("GOLD", "PRIZE_GOLD"),
            1 => ("RELIC", "PRIZE_RELIC"),
            2 => ("HEAL", "PRIZE_HEAL"),
            3 => ("CURSE", "PRIZE_CURSE"),
            4 => ("REMOVE", "PRIZE_REMOVE"),
            _ => ("DAMAGE", "PRIZE_DAMAGE")
        };

        SetEventState(PageDescription(pageKey), new[]
        {
            new EventOption(this,
                () => ApplyResult(result),
                $"{Id.Entry}.pages.RESULT.options.{optionKey}",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task ApplyResult(int result)
    {
        switch (result)
        {
            case 0:
                await PlayerCmd.GainGold(DynamicVars["GoldAmount"].IntValue, Owner);
                break;
            case 1:
                await RewardsCmd.OfferCustom(Owner, new List<Reward>
                {
                    new RelicReward(Owner)
                });
                break;
            case 2:
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                break;
            case 3:
                await CardPileCmd.AddCurseToDeck<Decay>(Owner);
                break;
            case 4:
                var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
                await CardPileCmd.RemoveFromDeck(
                    (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());
                break;
            default:
                await CreatureCmd.Damage(
                    new ThrowingPlayerChoiceContext(),
                    Owner.Creature,
                    DynamicVars["HpLoss"].BaseValue,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    null, null);
                break;
        }

        SetEventFinished(PageDescription("LEAVE"));
    }
}