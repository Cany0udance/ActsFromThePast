
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
}

*/