using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class TheWomanInBlue : CustomEventModel
{
    private const int Cost1 = 20;
    private const int Cost2 = 30;
    private const int Cost3 = 40;
    private const decimal PunchDmgPercent = 0.05M;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Cost1", Cost1),
        new IntVar("Cost2", Cost2),
        new IntVar("Cost3", Cost3),
        new IntVar("PunchDmg", 0)
    };

    public override void CalculateVars()
    {
        DynamicVars["PunchDmg"].BaseValue =
            (int)Math.Ceiling(Owner.Creature.MaxHp * PunchDmgPercent);
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Gold >= 50);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Buy1),
            Option(Buy2),
            Option(Buy3),
            Option(Leave).ThatDoesDamage(DynamicVars["PunchDmg"].BaseValue)
        };
    }

    private async Task Buy1()
    {
        await PlayerCmd.LoseGold(Cost1, Owner, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner, new List<Reward>
        {
            new PotionReward(Owner)
        });
        SetEventFinished(PageDescription("BUY"));
    }

    private async Task Buy2()
    {
        await PlayerCmd.LoseGold(Cost2, Owner, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner, new List<Reward>
        {
            new PotionReward(Owner),
            new PotionReward(Owner)
        });
        SetEventFinished(PageDescription("BUY"));
    }

    private async Task Buy3()
    {
        await PlayerCmd.LoseGold(Cost3, Owner, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner, new List<Reward>
        {
            new PotionReward(Owner),
            new PotionReward(Owner),
            new PotionReward(Owner)
        });
        SetEventFinished(PageDescription("BUY"));
    }

    private async Task Leave()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["PunchDmg"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        SetEventFinished(PageDescription("LEAVE"));
    }
}