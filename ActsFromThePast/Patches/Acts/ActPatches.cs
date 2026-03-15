using ActsFromThePast.Acts;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Patches.Acts;

public class ActPatches
{
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GetRandomList))]
    public class LegacyActsPatch
    {
    
        public static void Postfix(ref IEnumerable<ActModel> __result, string seed, UnlockState unlockState, bool isMultiplayer)
        {
            var list = __result.ToList();

            if (ActsFromThePastConfig.LegacyActsOnly)
            {
                list[0] = ModelDb.Act<ExordiumAct>();
                list[1] = ModelDb.Act<TheCityAct>();
                list[2] = ModelDb.Act<TheBeyondAct>();
            }
            else
            {
                var rng = new Rng((uint)StringHelper.GetDeterministicHashCode(seed + "_legacy_acts"));

                int act1Roll = rng.NextInt(3);
                if (act1Roll == 0)
                {
                    list[0] = ModelDb.Act<ExordiumAct>();
                }

                int act2Roll = rng.NextInt(2);
                if (act2Roll == 0)
                {
                    list[1] = ModelDb.Act<TheCityAct>();
                }

                int act3Roll = rng.NextInt(2);
                if (act3Roll == 0)
                {
                    list[2] = ModelDb.Act<TheBeyondAct>();
                }
            }

            __result = list;
        }
    }
    
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Acts), MethodType.Getter)]
    public static class ModelDb_Acts_Patch
    {
        public static void Postfix(ref IEnumerable<ActModel> __result)
        {
            var exordium = ModelDb.Act<ExordiumAct>();
            var city = ModelDb.Act<TheCityAct>();
            var beyond = ModelDb.Act<TheBeyondAct>();
        
            __result = __result
                .Append(exordium)
                .Append(city)
                .Append(beyond);
        }
    }
    
    [HarmonyPatch(typeof(AchievementsHelper), nameof(AchievementsHelper.CheckForDefeatedAllEnemiesAchievement))]
    public class SkipModdedActAchievementPatch
    {
        public static bool Prefix(ActModel act)
        {
            return act is not ExordiumAct and not TheCityAct and not TheBeyondAct;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.CreateRestSiteBackground))]
    public static class LegacyRestSitePatch
    {
        public static bool Prefix(ActModel __instance, ref Control __result)
        {
            string scenePath = __instance switch
            {
                ExordiumAct => "res://scenes/rest_site/overgrowth_rest_site.tscn",
                TheCityAct => "res://scenes/rest_site/hive_rest_site.tscn",
                TheBeyondAct => "res://scenes/rest_site/glory_rest_site.tscn",
                _ => null
            };
            
            if (scenePath == null)
                return true;
        
            __result = PreloadManager.Cache
                .GetScene(scenePath)
                .Instantiate<Control>();
        
            return false;
        }
    }
}