using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;
public sealed class ReactivePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

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
        if (props.HasFlag(ValueProp.Unpowered))
            return;
        if (!props.HasFlag(ValueProp.Move))
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (target.CurrentHp <= 0)
            return;
        
        Flash();
        if (Owner.Monster is MonsterModel monster)
        {
            var currentMoveId = monster.NextMove?.Id;
            var candidates = monster.MoveStateMachine.States.Values
                .Where(s => s.IsMove && s.Id != currentMoveId && s.Id != "MOVE_BRANCH")
                .ToList();
    
            if (candidates.Count > 0)
            {
                // Filter out one-time moves that have already been used
                if (monster is WrithingMass wm && wm.UsedMegaDebuff)
                    candidates.RemoveAll(s => s.Id == "MEGA_DEBUFF");
    
                if (candidates.Count > 0)
                {
                    var next = candidates[monster.RunRng.MonsterAi.NextInt(candidates.Count)];
                    monster.SetMoveImmediate((MoveState)next);
                }
            }
        }
    }
}