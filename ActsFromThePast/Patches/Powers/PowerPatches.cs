using ActsFromThePast.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Patches.Powers;

public class PowerPatches
{
    [HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
    public static class OddMushroomPatch
    {
        public static void Postfix(
            ref decimal __result,
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource)
        {
            if (target?.Player?.GetRelic<OddMushroom>() == null)
                return;

            if (__result == 1M)
                return;

            // Reduce the multiplier from 1.5x to 1.25x
            __result -= 0.25M;
        }
    }
}