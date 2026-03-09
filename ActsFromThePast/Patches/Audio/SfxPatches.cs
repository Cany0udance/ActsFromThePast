using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;

namespace ActsFromThePast.Patches.Audio;

public class SfxPatches
{
    
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.AfterApplied))]
    public class SneckoConfusedSfxPatch
    {
        public static void Postfix(PowerModel __instance, Creature? applier)
        {
            if (__instance is ConfusedPower && applier?.Monster is Snecko)
            {
                ModAudio.Play("snecko", "confusion_applied");
            }
        }
    }
}