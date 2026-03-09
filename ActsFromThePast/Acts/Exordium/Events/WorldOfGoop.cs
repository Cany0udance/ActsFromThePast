using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WorldOfGoop : EventModel
{
    private const int Damage = 11;
    private const int Gold = 75;
    private const int MinGoldLoss = 35;
    private const int MaxGoldLoss = 75;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("Damage", Damage),
                new GoldVar(Gold),
                new IntVar("GoldLoss", 0)
            };
        }
    }

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
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(GatherGoldOption),
                "WORLD_OF_GOOP.pages.INITIAL.options.GATHER",
                Array.Empty<IHoverTip>()).ThatDoesDamage(Damage),
            new EventOption(this, new Func<Task>(LeaveOption),
                "WORLD_OF_GOOP.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task GatherGoldOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            Damage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(L10NLookup("WORLD_OF_GOOP.pages.GATHER.description"));
    }

    private async Task LeaveOption()
    {
        await PlayerCmd.LoseGold(DynamicVars["GoldLoss"].BaseValue, Owner, GoldLossType.Lost);
        SetEventFinished(L10NLookup("WORLD_OF_GOOP.pages.LEAVE.description"));
    }
}