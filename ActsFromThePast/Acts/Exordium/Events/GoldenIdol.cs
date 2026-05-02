using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class GoldenIdol : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };
    private const decimal HpLossPercent = 0.35M;
    private const decimal MaxHpLossPercent = 0.10M;
    private const int JamGoldMin = 30;
    private const int JamGoldMax = 50;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Damage", 0),
        new IntVar("MaxHpLoss", 0),
        new GoldVar(0),
        new StringVar("Relic", "Relic")
    };

    public override void CalculateVars()
    {
        DynamicVars["Damage"].BaseValue = Math.Floor(Owner.Creature.MaxHp * HpLossPercent);
        var maxHpLoss = Math.Floor(Owner.Creature.MaxHp * MaxHpLossPercent);
        if (maxHpLoss < 1)
            maxHpLoss = 1;
        DynamicVars["MaxHpLoss"].BaseValue = maxHpLoss;
        DynamicVars.Gold.BaseValue = JamGoldMin + Rng.NextInt(JamGoldMax - JamGoldMin + 1);
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "golden_idol");
    }

    private IEnumerable<RelicModel> GetTradableRelics()
    {
        return Owner.Relics.Where(r =>
            !r.IsUsedUp &&
            !r.IsMelted &&
            !r.SpawnsPets &&
            r.Rarity != RelicRarity.Starter &&
            r.Rarity != RelicRarity.Event &&
            r.Rarity != RelicRarity.Ancient);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            var relic = Rng.NextItem(GetTradableRelics());
            if (relic != null)
            {
                ((StringVar)DynamicVars["Relic"]).StringValue = relic.Title.GetFormattedText();
                return new[]
                {
                    Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
                    new EventOption(this, async () => await Switcheroo(relic),
                        L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO.title"),
                        L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO.description"),
                        $"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO",
                        relic.HoverTips).ThatHasDynamicTitle()
                };
            }

            return new[]
            {
                Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
                new EventOption(this, null,
                    $"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO_LOCKED",
                    Array.Empty<IHoverTip>())
            };
        }

        return new[]
        {
            Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
            Option(Leave)
        };
    }

    private async Task Take()
    {
        var relic = ModelDb.Relic<Relics.GoldenIdol>().ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            SetEventState(PageDescription("BOULDER_REBALANCED"), new[]
            {
                Option(Jam, "BOULDER_REBALANCED"),
                Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
                Option(Crawl, "BOULDER")
            });
        }
        else
        {
            SetEventState(PageDescription("BOULDER"), new[]
            {
                Option(Outrun, "BOULDER", HoverTipFactory.FromCard(ModelDb.Card<Injury>())),
                Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
                Option(Crawl, "BOULDER")
            });
        }
    }

    private async Task Switcheroo(RelicModel relic)
    {
        await RelicCmd.Remove(relic);
        await RelicCmd.Obtain(ModelDb.Relic<Relics.GoldenIdol>().ToMutable(), Owner);
        SetEventFinished(PageDescription("SWITCHEROO"));
    }

    private async Task Jam()
    {
        var goldenIdol = Owner.Relics.FirstOrDefault(r => r is Relics.GoldenIdol);
        if (goldenIdol != null)
            await RelicCmd.Remove(goldenIdol);

        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("JAM"));
    }

    private async Task Outrun()
    {
        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
        SetEventFinished(PageDescription("OUTRUN"));
    }

    private async Task Smash()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(PageDescription("SMASH"));
    }

    private async Task Crawl()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MaxHpLoss"].BaseValue,
            false);
        SetEventFinished(PageDescription("CRAWL"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}