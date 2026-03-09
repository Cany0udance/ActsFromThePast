using ActsFromThePast.Acts.TheBeyond.Enemies;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Powers;

public sealed class LifeLinkPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData() => new Data();

    private bool IsReviving => GetInternalData<Data>().isReviving;

    public async Task DoReattach()
    {
        if (AreAllOtherDarklingsDead())
            return;

        GetInternalData<Data>().isReviving = false;
        NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, true);
        await CreatureCmd.TriggerAnim(Owner, "Revive", 0.0f);
        await CreatureCmd.Heal(Owner, Amount);
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner != creature)
            return;

        if (!AreAllOtherDarklingsDead() || !Owner.IsDead)
        {
            GetInternalData<Data>().isReviving = true;
            if (creature.Monster is Darkling darkling)
                Owner.Monster.SetMoveImmediate(darkling.DeadState);
            await CreatureCmd.TriggerAnim(Owner, "Dead", 0.0f);
            NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, false);
        }
    }

    public override bool ShouldAllowHitting(Creature creature)
    {
        return creature != Owner || !IsReviving;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature != Owner;
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    public override bool ShouldOwnerDeathTriggerFatal() => AreAllOtherDarklingsDead();

    private IEnumerable<Creature> GetOtherDarklings()
    {
        return Owner.CombatState.GetTeammatesOf(Owner)
            .Where(c => c != Owner && c.Monster is Darkling);
    }

    private bool AreAllOtherDarklingsDead()
    {
        return GetOtherDarklings().All(s => s.IsDead);
    }

    private class Data
    {
        public bool isReviving;
    }
}