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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Acts;

public class ActBackgroundPatches
{
    private static readonly HashSet<string> LegacyActTitles = new() { "exordium_act", "the_city_act", "the_beyond_act" };
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GetAllBackgroundLayerPaths))]
    public class LegacyBackgroundLayersPatch
    {
        public static bool Prefix(ActModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is ExordiumAct or TheCityAct or TheBeyondAct)
            {
                __result = Array.Empty<string>();
                return false;
            }
            return true;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    public class LegacyGenerateBackgroundAssetsPatch
    {
        public static bool Prefix(ActModel __instance, ref BackgroundAssets __result)
        {
            string title = __instance switch
            {
                ExordiumAct => "exordium_act",
                TheCityAct => "the_city_act",
                TheBeyondAct => "the_beyond_act",
                _ => null
            };
        
            if (title == null)
                return true;
        
            __result = CreateLegacyBackgroundAssets(title);
            return false;
        }
    
        private static BackgroundAssets CreateLegacyBackgroundAssets(string title)
        {
            var instance = (BackgroundAssets)FormatterServices.GetUninitializedObject(typeof(BackgroundAssets));
        
            var type = typeof(BackgroundAssets);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        
            type.GetField("<BackgroundScenePath>k__BackingField", flags)
                ?.SetValue(instance, SceneHelper.GetScenePath($"backgrounds/{title}/{title}_background"));
            type.GetField("<BgLayers>k__BackingField", flags)
                ?.SetValue(instance, new List<string>());
            type.GetField("<FgLayer>k__BackingField", flags)
                ?.SetValue(instance, "");
        
            return instance;
        }
    }
    
    [HarmonyPatch(typeof(NCombatBackground), nameof(NCombatBackground.Create))]
    public class LegacyBackgroundCreatePatch
    {
        public static bool Prefix(BackgroundAssets bg, ref NCombatBackground __result)
        {
            if (bg.BackgroundScenePath == null)
                return true;
            
            NCombatBackground background = null;
            string name = null;
            
            if (bg.BackgroundScenePath.Contains("exordium_act"))
            {
                background = new ExordiumBackground();
                name = "ExordiumActBackground";
            }
            else if (bg.BackgroundScenePath.Contains("the_city_act"))
            {
                background = new TheCityBackground();
                name = "TheCityActBackground";
            }
            else if (bg.BackgroundScenePath.Contains("the_beyond_act"))
            {
                background = new TheBeyondBackground();
                name = "TheBeyondActBackground";
            }
            
            if (background == null)
                return true;
            
            background.Name = name;
            
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
            
            return false;
        }
    }
}