using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public sealed class SporeCloudPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner)
            return;

        var players = CombatState.PlayerCreatures.Where(c => c.IsAlive);
        
        if (!players.Any())
            return;

        Flash();
        ModAudio.Play("fungi_beast", "spore_cloud_release");

        foreach (var player in players)
        {
            await PowerCmd.Apply<VulnerablePower>(player, Amount, null, null);
        }
    }
}