using ActsFromThePast.Minigames;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace ActsFromThePast.Patches.Minigames;

public class MinigamePatches
{
    [HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
    public static class NCardHolder_CreateHoverTips_Patch
    {
        public static bool Prefix(NCardHolder __instance)
        {
            var cardNode = __instance.CardNode;
            if (cardNode != null && !cardNode.Visible)
            {
                // Only suppress if we're inside the match & keep screen
                Node parent = __instance;
                while (parent != null)
                {
                    if (parent is NMatchAndKeepScreen)
                        return false;
                    parent = parent.GetParent();
                }
            }
            return true;
        }
    }
}