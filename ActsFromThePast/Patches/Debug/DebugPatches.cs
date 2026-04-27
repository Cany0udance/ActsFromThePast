
/*

using HarmonyLib;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Debug;

public class DebugPatches
{
    [HarmonyPatch(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll))]
    public static class ForceShopPatch
    {
        public static bool Prefix(ref RoomType __result)
        {
            __result = RoomType.Shop;
            return false;
        }
    }
    
    
        [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Add), new[] { typeof(Creature) })]
    public static class CreatureAddPositionLogger
    {
        public static void Postfix(Creature creature)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (node == null) return;
            Log.Info($"[CreatureAdd] {creature.Monster?.GetType().Name} at {node.GlobalPosition}");
        }
    }
}

*/