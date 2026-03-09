using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SensoryStone : EventModel
{
    private const int Dmg2 = 5;
    private const int Dmg3 = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Dmg2", Dmg2),
                new IntVar("Dmg3", Dmg3)
            };
        }
    }
    
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "sensory_stone");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(this, IntroOption,
                "SENSORY_STONE.pages.INITIAL.options.CONTINUE")
        };
    }

    private Task IntroOption()
    {
        SetEventState(
            L10NLookup("SENSORY_STONE.pages.INTRO_2.description"),
            new[]
            {
                new EventOption(this, () => MemoryOption(1),
                    "SENSORY_STONE.pages.INTRO_2.options.MEMORY_1"),
                new EventOption(this, () => MemoryOption(2),
                    "SENSORY_STONE.pages.INTRO_2.options.MEMORY_2").ThatDoesDamage(Dmg2),
                new EventOption(this, () => MemoryOption(3),
                    "SENSORY_STONE.pages.INTRO_2.options.MEMORY_3").ThatDoesDamage(Dmg3)
            }
        );
        return Task.CompletedTask;
    }

    private async Task MemoryOption(int choice)
    {
        
        // TODO add 50/50 chance for rare colorless
        
        if (choice == 2)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                Dmg2,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }
        else if (choice == 3)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                Dmg3,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }

        var memoryKey = GetRandomMemoryKey();

        var rewards = new List<Reward>(choice);
        for (int i = 0; i < choice; i++)
        {
            rewards.Add(new CardReward(
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    new[] { (CardPoolModel)ModelDb.CardPool<ColorlessCardPool>() }),
                3, Owner));
        }
        await RewardsCmd.OfferCustom(Owner, rewards);

        SetEventFinished(L10NLookup(memoryKey));
    }

    private string GetRandomMemoryKey()
    {
        var keys = new[]
        {
            "SENSORY_STONE.pages.MEMORY_1.description",
            "SENSORY_STONE.pages.MEMORY_2.description",
            "SENSORY_STONE.pages.MEMORY_3.description",
            "SENSORY_STONE.pages.MEMORY_4.description"
        };
        return Rng.NextItem(keys);
    }
}