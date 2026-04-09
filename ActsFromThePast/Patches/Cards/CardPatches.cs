using System.Reflection;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

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

[HarmonyPatch(typeof(CardModel), "ToMutable")]
public class TagClassicSlimedPatch
{
    public static void Postfix(CardModel __result)
    {
        if (__result is Slimed && ClassicSlimedTracker.CreatingClassicSlimed)
        {
            ClassicSlimedTracker.IsClassicSlimed.Set(__result, true);
        }
    }
}

[HarmonyPatch]
public class ClassicSlimedDescriptionPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            new[] { typeof(PileType), AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"), typeof(Creature) });
    }

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Slimed)
            return;
        if (!ClassicSlimedTracker.IsClassicSlimed.Get(__instance))
            return;

        var desc = __instance.Description;
        __instance.DynamicVars.AddTo(desc);
        var descText = desc.GetFormattedText();

        __result = __result.Replace(descText, "").Trim('\n');
    }
}

[HarmonyPatch(typeof(Slimed), "OnPlay")]
public class ClassicSlimedOnPlayPatch
{
    public static bool Prefix(Slimed __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        if (!ClassicSlimedTracker.IsClassicSlimed.Get(__instance))
            return true;

        var child = NGoopyImpactVfx.Create(__instance.Owner.Creature);
        if (child != null)
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(child);

        __result = Task.CompletedTask;
        return false;
    }
}