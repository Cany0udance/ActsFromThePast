using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;


// Should be unavailable in multiplayer due to hungry face
public sealed class FaceTrader : CustomEventModel
{
    private const int GoldReward = 50;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Damage", 0),
        new GoldVar(GoldReward)
    };

    public override void CalculateVars()
    {
        var dmg = (int)(Owner.Creature.MaxHp / 10M);
        if (dmg == 0) dmg = 1;
        DynamicVars["Damage"].BaseValue = dmg;
    }
    
    public override bool IsAllowed(IRunState runState)
    {
        if (runState.Players.Count > 1)
            return false;
        
        // Unavailable in multiplayer. N'loth's Hungry ahh Face can cause issues

        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
        {
            var dmg = (int)(p.Creature.MaxHp / 10M);
            if (dmg == 0) dmg = 1;
            return p.Creature.CurrentHp > dmg;
        });
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        var options = new List<EventOption>
        {
            Option(Touch, "MAIN").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
            Option(Trade, "MAIN")
        };

        if (!ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Leave, "MAIN"));
        }

        SetEventState(PageDescription("MAIN"), options);
        return Task.CompletedTask;
    }

    private async Task Touch()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        await PlayerCmd.GainGold(GoldReward, Owner);
        SetEventFinished(PageDescription("TOUCH"));
    }

    private async Task Trade()
    {
        var relic = GetRandomFace();
        await RelicCmd.Obtain(relic.ToMutable(), Owner);
        SetEventFinished(PageDescription("TRADE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private RelicModel GetRandomFace()
    {
        var candidates = new List<RelicModel>();

        if (!Owner.Relics.Any(r => r is CultistHeadpiece))
            candidates.Add(ModelDb.Relic<CultistHeadpiece>());
        if (!Owner.Relics.Any(r => r is FaceOfCleric))
            candidates.Add(ModelDb.Relic<FaceOfCleric>());
        if (!Owner.Relics.Any(r => r is GremlinVisage))
            candidates.Add(ModelDb.Relic<GremlinVisage>());
        if (!Owner.Relics.Any(r => r is NlothsHungryFace))
            candidates.Add(ModelDb.Relic<NlothsHungryFace>());
        if (!Owner.Relics.Any(r => r is SsserpentHead))
            candidates.Add(ModelDb.Relic<SsserpentHead>());

        if (candidates.Count == 0)
            return ModelDb.Relic<Circlet>();

        return Rng.NextItem(candidates);
    }
}