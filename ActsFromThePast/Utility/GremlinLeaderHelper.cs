using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast;

public static class GremlinLeaderHelper
{
    private static readonly LocString _fleeLine1 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee1");
    private static readonly LocString _fleeLine2 = MonsterModel.L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_LEADER.gremlinFlee2");

    public static void SubscribeToLeaderDeath(Creature gremlin, CombatState combatState)
    {
        var leader = combatState.GetTeammatesOf(gremlin)
            .FirstOrDefault(t => t.Monster is GremlinLeader);
        if (leader == null)
            return;

        leader.Died += OnLeaderDied;

        async void OnLeaderDied(Creature _)
        {
            leader.Died -= OnLeaderDied;
            if (gremlin.IsDead)
                return;

            var livingGremlins = combatState.GetTeammatesOf(gremlin)
                .Where(t => t != leader && t.IsAlive)
                .ToList();
            var isFirst = livingGremlins.FirstOrDefault() == gremlin;

            var line = isFirst ? _fleeLine1 : _fleeLine2;
            TalkCmd.Play(line, gremlin, VfxColor.White, VfxDuration.Short);

            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(gremlin);
            creatureNode?.ToggleIsInteractable(false);

            await CreatureCmd.Escape(gremlin, removeCreatureNode: false);

            // Remove from escaped list so it doesn't affect rewards
            combatState.EscapedCreatures.Remove(gremlin);

            await EscapeAnimation.Play(gremlin);
            if (creatureNode != null)
            {
                creatureNode.Visible = false;
                NCombatRoom.Instance?.RemoveCreatureNode(creatureNode);
            }
        }
    }
}