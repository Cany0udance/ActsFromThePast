using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Powers;

public sealed class FadingPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        if (Amount <= 1)
        {
            if (!Owner.IsDead)
            {
                Flash();
                var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
                if (creatureNode != null)
                {
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
                        NFireSmokePuffVfx.Create(Owner));
                }
                await Cmd.Wait(0.1f);
                await CreatureCmd.Kill(Owner);
            }
        }
        else
        {
            Flash();
            await PowerCmd.Decrement(this);
        }
    }
}