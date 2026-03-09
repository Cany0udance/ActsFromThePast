using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

public sealed class MutagenicStrength : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new PowerVar<StrengthPower>(3M)
            };
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return new IHoverTip[]
            {
                HoverTipFactory.FromPower<StrengthPower>()
            };
        }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;

        Flash();
        
        // Apply permanent strength
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature, 
            DynamicVars.Strength.BaseValue, 
            Owner.Creature, 
            null);
        
        // Apply temporary strength loss (will remove the strength at end of turn)
        await PowerCmd.Apply<MutagenicStrengthPower>(
            Owner.Creature, 
            DynamicVars.Strength.BaseValue, 
            Owner.Creature, 
            null);
    }
}