using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class PlatedArmorPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return new IHoverTip[]
            {
                HoverTipFactory.Static(StaticHoverTip.Block)
            };
        }
    }
    
    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        ICombatState combatState)
    {
        if (Owner.Side != CombatSide.Enemy || side != CombatSide.Player || combatState.RoundNumber != 1)
            return Task.CompletedTask;
        return CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, (decimal)Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return;
        Flash();
        await PowerCmd.Decrement(this);
    
        if (Amount <= 0 && Owner.Monster is ShelledParasite shelledParasite)
        {
            await shelledParasite.OnArmorBreak();
        }
    }
}