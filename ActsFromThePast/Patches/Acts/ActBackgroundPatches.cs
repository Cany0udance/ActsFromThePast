using System.Reflection;
using System.Runtime.Serialization;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.Exordium;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Acts;

public static class LegacyActTracker
{
    public static readonly Dictionary<BackgroundAssets, string> LegacyBackgrounds = new();
    public static bool IsCollectorEncounter { get; set; }
}

public class ActBackgroundPatches
{
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GetAllBackgroundLayerPaths))]
    public class LegacyBackgroundLayersPatch
    {
        public static bool Prefix(ActModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is not ExordiumAct and not TheCityAct and not TheBeyondAct)
                return true;

            __result = Array.Empty<string>();
            return false;
        }
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    public class LegacyGenerateBackgroundAssetsPatch
    {
        public static bool Prefix(ActModel __instance, ref BackgroundAssets __result)
        {
            if (__instance is not ExordiumAct and not TheCityAct and not TheBeyondAct)
                return true;

            __result = CreateLegacyBackgroundAssets(__instance);
            return false;
        }

        private static BackgroundAssets CreateLegacyBackgroundAssets(ActModel act)
        {
            var instance = (BackgroundAssets)FormatterServices.GetUninitializedObject(typeof(BackgroundAssets));

            var type = typeof(BackgroundAssets);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            type.GetField("<BackgroundScenePath>k__BackingField", flags)
                ?.SetValue(instance, "");
            type.GetField("<BgLayers>k__BackingField", flags)
                ?.SetValue(instance, new List<string>());
            type.GetField("<FgLayer>k__BackingField", flags)
                ?.SetValue(instance, "");

            LegacyActTracker.LegacyBackgrounds[instance] = act switch
            {
                ExordiumAct => "exordium_act",
                TheCityAct => "the_city_act",
                TheBeyondAct => "the_beyond_act",
                _ => ""
            };

            return instance;
        }
    }

    [HarmonyPatch]
    public class LegacyEncounterBackgroundPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EncounterModel), "GetBackgroundAssets", new[] { typeof(ActModel), typeof(Rng) });
        }

        public static bool Prefix(EncounterModel __instance, ActModel parentAct, Rng rng, ref BackgroundAssets __result)
        {
            if (parentAct is not ExordiumAct and not TheCityAct and not TheBeyondAct)
                return true;

            // Skip encounters that have their own unique backgrounds.
            // If more cases pop up, consider switching to an allowlist instead:
            //   if (__instance is not CombatEncounterModel) return true;
            // That would let only regular combat encounters through and automatically
            // exclude any future special encounters (events, endings, etc.).
            if (__instance is TheArchitectEventEncounter)
                return true;

            var instance = (BackgroundAssets)FormatterServices.GetUninitializedObject(typeof(BackgroundAssets));
            var type = typeof(BackgroundAssets);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            type.GetField("<BackgroundScenePath>k__BackingField", flags)
                ?.SetValue(instance, "");
            type.GetField("<BgLayers>k__BackingField", flags)
                ?.SetValue(instance, new List<string>());
            type.GetField("<FgLayer>k__BackingField", flags)
                ?.SetValue(instance, "");

            LegacyActTracker.LegacyBackgrounds[instance] = parentAct switch
            {
                ExordiumAct => "exordium_act",
                TheCityAct => "the_city_act",
                TheBeyondAct => "the_beyond_act",
                _ => ""
            };
            
            if (parentAct is TheCityAct)
                LegacyActTracker.IsCollectorEncounter = __instance is CollectorBoss;

            __result = instance;
            return false;
        }
    }

    [HarmonyPatch(typeof(NCombatBackground), nameof(NCombatBackground.Create))]
    public class LegacyBackgroundCreatePatch
    {
        public static bool Prefix(BackgroundAssets bg, ref NCombatBackground __result)
        {
            if (!LegacyActTracker.LegacyBackgrounds.TryGetValue(bg, out var actId))
                return true;

            NCombatBackground background = actId switch
            {
                "exordium_act" => new ExordiumBackground(),
                "the_city_act" => new TheCityBackground(),
                "the_beyond_act" => new TheBeyondBackground(),
                _ => null
            };

            if (background == null)
                return true;

            background.Name = $"{actId}_background";

            for (int i = 0; i < 4; i++)
            {
                var layer = new Control();
                layer.Name = $"Layer_{i:D2}";
                background.AddChild(layer);
            }

            var foreground = new Control();
            foreground.Name = "Foreground";
            background.AddChild(foreground);

            if (background is ExordiumBackground exordium)
                exordium.TreeEntered += exordium.OnTreeEntered;
            else if (background is TheCityBackground city)
                city.TreeEntered += city.OnTreeEntered;
            else if (background is TheBeyondBackground beyond)
                beyond.TreeEntered += beyond.OnTreeEntered;

            __result = background;
            LegacyActTracker.LegacyBackgrounds.Remove(bg);
            return false;
        }
    }
}