using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast.Patches.LogSuppressors;

[HarmonyPatch]
public static class LogSuppressorPatches
{
    private static readonly HashSet<string> ModdedEnemies = new()
    {
        // Exordium Enemies
        "AcidSlimeLarge", "AcidSlimeMedium", "AcidSlimeSmall",
        "Cultist", "FungiBeast",
        "GremlinFat", "GremlinMad", "GremlinShield", "GremlinSneaky", "GremlinWizard",
        "JawWorm", "Looter", "LouseGreen", "LouseRed",
        "SlaverBlue", "SlaverRed",
        "SpikeSlimeLarge", "SpikeSlimeMedium", "SpikeSlimeSmall",

        // Exordium Elites
        "GremlinNob", "Lagavulin", "Sentry",

        // Exordium Bosses
        "Guardian", "Hexaghost", "SlimeBoss",

        // City Enemies
        "Byrd", "Centurion", "Mugger", "Mystic", "Chosen",
        "ShelledParasite", "SnakePlant", "SphericGuardian",
        "Pointy", "Romeo", "Bear",

        // City Elites
        "Taskmaster", "BookOfStabbing", "GremlinLeader",

        // City Bosses
        "TorchHead", "Collector", "Champ", "BronzeAutomaton", "BronzeOrb",

        // Beyond Enemies
        "Darkling", "Exploder", "Maw", "OrbWalker",
        "Repulsor", "Spiker", "SpireGrowth", "Transient", "WrithingMass",

        // Beyond Elites
        "GiantHead", "Nemesis", "Reptomancer", "SnakeDagger",

        // Beyond Bosses
        "AwakenedOne", "Donu", "Deca"
    };

    private static MegaSprite GetSpineController(CreatureAnimator instance)
    {
        return Traverse.Create(instance).Field("_spineController").GetValue<MegaSprite>();
    }

    private static bool IsModdedEnemy(MegaSprite spineController)
    {
        var parent = (spineController?.BoundObject as Node)?.GetParent();
        var name = parent?.Name.ToString() ?? "";
        return ModdedEnemies.Contains(name);
    }


    [HarmonyPatch(typeof(CreatureAnimator), "SetNextState")]
    [HarmonyPrefix]
    public static bool SetNextStatePrefix(CreatureAnimator __instance, AnimState state)
    {
        var spine = GetSpineController(__instance);
        if (!IsModdedEnemy(spine))
            return true;

        // Mirror the original's unconditional assignment
        Traverse.Create(__instance).Field("_currentState").SetValue(state);

        if (!spine.HasAnimation(state.Id))
            return false; // silently skip instead of logging

        return true;
    }

    [HarmonyPatch(typeof(CreatureAnimator), "AddNextState")]
    [HarmonyPrefix]
    public static bool AddNextStatePrefix(CreatureAnimator __instance, AnimState state)
    {
        var spine = GetSpineController(__instance);
        if (!IsModdedEnemy(spine))
            return true;

        if (!spine.HasAnimation(state.Id))
            return false;

        return true;
    }
}