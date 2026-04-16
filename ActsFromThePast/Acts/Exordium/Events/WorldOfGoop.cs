using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WorldOfGoop : CustomEventModel
{
    private const int Damage = 11;
    private const int Gold = 75;
    private const int MinGoldLoss = 35;
    private const int MaxGoldLoss = 75;

    public override ActModel[] Acts => new[] { ModelDb.Act<ExordiumAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Damage", Damage),
        new GoldVar(Gold),
        new IntVar("GoldLoss", 0)
    };

    public override void CalculateVars()
    {
        var goldLoss = MinGoldLoss + Rng.NextInt(MaxGoldLoss - MinGoldLoss + 1);
        if (goldLoss > Owner.Gold)
            goldLoss = Owner.Gold;
        DynamicVars["GoldLoss"].BaseValue = goldLoss;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "spirits");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Gather).ThatDoesDamage(Damage),
            Option(Leave)
        };
    }

    private async Task Gather()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            Damage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("GATHER"));
    }

    private async Task Leave()
    {
        await PlayerCmd.LoseGold(DynamicVars["GoldLoss"].BaseValue, Owner, GoldLossType.Lost);
        SetEventFinished(PageDescription("LEAVE"));
    }
}