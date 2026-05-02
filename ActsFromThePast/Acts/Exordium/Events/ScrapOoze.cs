using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ScrapOoze : CustomEventModel
{
    private const int BaseDamage = 5;
    private const int BaseRelicChance = 25;
    private const int ChanceIncreasePerAttempt = 10;
    private const int DamageIncreasePerAttempt = 1;
    private const int BloatGoldMin = 90;
    private const int BloatGoldMax = 130;
    private int _attempts;

    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    private int Attempts
    {
        get => _attempts;
        set
        {
            AssertMutable();
            _attempts = value;
        }
    }

    private int CurrentDamage => BaseDamage + Attempts * DamageIncreasePerAttempt;
    private int CurrentRelicChance => BaseRelicChance + Attempts * ChanceIncreasePerAttempt;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Damage", CurrentDamage),
        new IntVar("RelicChance", CurrentRelicChance),
        new GoldVar(BloatGoldMax)
    };

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = BloatGoldMax - Rng.NextInt(BloatGoldMax - BloatGoldMin + 1);
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "scrap_ooze");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Reach).ThatDoesDamage(CurrentDamage),
                Option(Bloat, "INITIAL_REBALANCED")
            };
        }

        return new[]
        {
            Option(Reach).ThatDoesDamage(CurrentDamage),
            Option(Leave)
        };
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return runState.Players.All<Player>(p => p.Gold >= DynamicVars.Gold.BaseValue);

        return true;
    }

    private async Task Reach()
    {
        // TODO poison sfx when it exists
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            CurrentDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        int roll = Rng.NextInt(100);
        int threshold = 100 - CurrentRelicChance;

        if (roll >= threshold)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
            await RelicCmd.Obtain(relic, Owner);
            SetEventFinished(PageDescription("SUCCESS"));
        }
        else
        {
            Attempts++;
            DynamicVars["Damage"].BaseValue = CurrentDamage;
            DynamicVars["RelicChance"].BaseValue = CurrentRelicChance;
            SetEventState(PageDescription("FAIL"), new[]
            {
                Option(Reach, "FAIL").ThatDoesDamage(CurrentDamage),
                Option(Leave, "FAIL")
            });
        }
    }

    private async Task Bloat()
    {
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, Owner, GoldLossType.Spent);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_die");
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("BLOAT"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}