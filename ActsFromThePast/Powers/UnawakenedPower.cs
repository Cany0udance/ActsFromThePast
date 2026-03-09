using ActsFromThePast.Acts.TheBeyond.Enemies;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class UnawakenedPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData() => new Data();

    private bool IsReviving => GetInternalData<Data>().isReviving;

    public void DoRevive() => GetInternalData<Data>().isReviving = false;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner || !(creature.Monster is AwakenedOne monster))
            return;
        GetInternalData<Data>().isReviving = true;
        await monster.TriggerDeadState();
    }

    public override bool ShouldAllowHitting(Creature creature)
    {
        return creature != Owner || !IsReviving;
    }

    public override bool ShouldStopCombatFromEnding()
    {
        if (Owner?.Monster is AwakenedOne awakened)
        {
            return !awakened.ShouldDisappearFromDoom || Owner.IsDead;
        }
        return false;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature != Owner;
    }
    
    public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
    {
        return power.Type == PowerType.Debuff;
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    public override bool ShouldOwnerDeathTriggerFatal() => false;

    private class Data
    {
        public bool isReviving;
    }
}