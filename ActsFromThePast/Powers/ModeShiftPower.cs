using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ModeShiftPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (Owner.Monster is not Guardian guardian)
            return;
        if (!guardian.IsOpen || guardian.CloseUpTriggered)
            return;
        if (Owner.IsDead)
            return;

        guardian.DmgTaken += result.UnblockedDamage;

        // Update the power display
        SetAmount(Math.Max(0, Amount - result.UnblockedDamage));

        if (guardian.DmgTaken >= guardian.CurrentDmgThreshold)
        {
            Flash();
    
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
            if (creatureNode != null)
            {
                var vfx = IntenseZoomEffect.Create(creatureNode.VfxSpawnPosition, false);
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
            }
    
            guardian.DmgTaken = 0;
            guardian.CloseUpTriggered = true;
            await guardian.TransitionToDefensiveMode();
        }
    }
}