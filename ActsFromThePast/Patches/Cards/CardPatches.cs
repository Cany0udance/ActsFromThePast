using System.Reflection;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Patches.Cards;
[HarmonyPatch(typeof(Burn))]
public static class BurnUpgradePatch
{
    public static bool AllowBurnUpgrade = false;

    [HarmonyPatch(nameof(Burn.MaxUpgradeLevel), MethodType.Getter)]
    [HarmonyPostfix]
    static void MaxUpgradeLevel_Postfix(ref int __result)
    {
        __result = AllowBurnUpgrade ? 1 : 0;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
    [HarmonyPostfix]
    static void UpgradeInternal_Postfix(CardModel __instance)
    {
        if (__instance is Burn burn && burn.IsUpgraded)
        {
            burn.DynamicVars.Damage.UpgradeValueBy(2M);
        }
    }
    
}