using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ExplosivePower : CustomPowerModel
{
    private const int ExplodeDamage = 30;

    public override PowerType Type => PowerType.Buff;
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
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
                    NFireSmokePuffVfx.Create(Owner));
                await Cmd.Wait(0.1f);
                foreach (var player in Owner.CombatState?.Players ?? Enumerable.Empty<Player>())
                {
                    await CreatureCmd.Damage(choiceContext, player.Creature, ExplodeDamage, ValueProp.Unpowered, Owner, null);
                }
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