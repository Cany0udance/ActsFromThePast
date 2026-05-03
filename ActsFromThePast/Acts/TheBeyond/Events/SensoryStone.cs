using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SensoryStone : CustomEventModel
{
    private const int Dmg2 = 5;
    private const int Dmg3 = 10;
    private const int Dmg2Rebalanced = 10;
    private const int Dmg3Rebalanced = 20;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Dmg2", ActsFromThePastConfig.RebalancedMode ? Dmg2Rebalanced : Dmg2),
        new IntVar("Dmg3", ActsFromThePastConfig.RebalancedMode ? Dmg3Rebalanced : Dmg3)
    };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "sensory_stone");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        int dmg2 = ActsFromThePastConfig.RebalancedMode ? Dmg2Rebalanced : Dmg2;
        int dmg3 = ActsFromThePastConfig.RebalancedMode ? Dmg3Rebalanced : Dmg3;

        SetEventState(PageDescription("INTRO_2"), new[]
        {
            new EventOption(this, () => Memory(1),
                $"{Id.Entry}.pages.INTRO_2.options.MEMORY_1",
                Array.Empty<IHoverTip>()),
            new EventOption(this, () => Memory(2),
                $"{Id.Entry}.pages.INTRO_2.options.MEMORY_2",
                Array.Empty<IHoverTip>()).ThatDoesDamage(dmg2),
            new EventOption(this, () => Memory(3),
                $"{Id.Entry}.pages.INTRO_2.options.MEMORY_3",
                Array.Empty<IHoverTip>()).ThatDoesDamage(dmg3)
        });
        return Task.CompletedTask;
    }

    private async Task Memory(int choice)
    {
        // TODO add 50/50 chance for rare colorless
        int dmg2 = ActsFromThePastConfig.RebalancedMode ? Dmg2Rebalanced : Dmg2;
        int dmg3 = ActsFromThePastConfig.RebalancedMode ? Dmg3Rebalanced : Dmg3;

        if (choice == 2)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                dmg2,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }
        else if (choice == 3)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                dmg3,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }

        var memoryText = GetRandomMemoryText();
        var rewards = new List<Reward>(choice);
        for (int i = 0; i < choice; i++)
        {
            rewards.Add(new CardReward(
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    new[] { (CardPoolModel)ModelDb.CardPool<ColorlessCardPool>() }),
                3, Owner));
        }

        await RewardsCmd.OfferCustom(Owner, rewards);
        SetEventFinished(memoryText);
    }

    private LocString GetRandomMemoryText()
    {
        var keys = new[]
        {
            $"{Id.Entry}.pages.MEMORY_1.description",
            $"{Id.Entry}.pages.MEMORY_2.description",
            $"{Id.Entry}.pages.MEMORY_3.description",
            $"{Id.Entry}.pages.MEMORY_4.description"
        };
        return L10NLookup(Rng.NextItem(keys));
    }
}