using ActsFromThePast.Acts.TheBeyond.Enemies;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

namespace ActsFromThePast.Patches.Creatures;

public class VisualsPatches
{
    [HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals.SetScaleAndHue))]
    public class SetScaleAndHuePatch
    {
        private const string LOG_TAG = "[ActsFromThePast]";

        private static readonly HashSet<string> _moddedCreatureTypes = new()
        {
            // Exordium Enemies
        
            "AcidSlimeLarge",
            "AcidSlimeMedium",
            "AcidSlimeSmall",
            "Cultist",
            "FungiBeast",
            "GremlinFat",
            "GremlinMad",
            "GremlinShield",
            "GremlinSneaky",
            "GremlinWizard",
            "JawWorm",
            "Looter",
            "LouseGreen",
            "LouseRed",
            "SlaverBlue",
            "SlaverRed",
            "SpikeSlimeLarge",
            "SpikeSlimeMedium",
            "SpikeSlimeSmall",
        
            // Exordium Elites
        
            "GremlinNob",
            "Lagavulin",
            "Sentry",
        
            // Exordium Bosses
        
            "Guardian",
            "Hexaghost",
            "SlimeBoss",
            
            // City Enemies
            
            "Byrd",
            "Centurion",
            "Mugger",
            "Mystic",
            "Chosen",
            "ShelledParasite",
            "SnakePlant",
            "SphericGuardian",
            "Pointy",
            "Romeo",
            "Bear",
                
            // City Elites
                
            "Taskmaster",
            "BookOfStabbing",
            "GremlinLeader",
                
            // City Bosses
            
            "TorchHead",
            "Collector",
            "Champ",
            "BronzeAutomaton",
            "BronzeOrb",
            
            // Beyond Enemies
            
            "Darkling",
            "Exploder",
            "Maw",
            "OrbWalker",
            "Repulsor",
            "Spiker",
            "SpireGrowth",
            "Transient",
            "WrithingMass",
            
            // Beyond Elites
            
            "GiantHead",
            "Nemesis",
            "Reptomancer",
            "SnakeDagger",
            
            // Beyond Bosses
            
            "AwakenedOne",
            "Donu",
            "Deca"
        };

        public static bool Prefix(NCreatureVisuals __instance, float scale, float hue)
        {
            var creatureName = __instance.Name;
        
            if (_moddedCreatureTypes.Contains(creatureName))
            {
                return false;
            }
        
            return true;
        }
    }
    
    [HarmonyPatch(typeof(NCombatRoom), "PositionEnemies")]
    public static class CreaturePositionPatch
    {
        public static void Postfix(List<NCreature> creatures)
        {
            foreach (var node in creatures)
            {
                var offset = node.Entity.Monster switch
                {
                    
                    /*
                    
                    // Exordium Enemies

                    AcidSlimeLarge => 30f,
                    AcidSlimeMedium => 30f,
                    AcidSlimeSmall => 30f,
                    Cultist => 30f,
                    FungiBeast => 30f,
                    GremlinFat => 30f,
                    GremlinMad => 30f,
                    GremlinShield => 30f,
                    GremlinSneaky => 30f,
                    GremlinWizard => 30f,
                    JawWorm => 30f,
                    Looter => 30f,
                    LouseGreen => 30f,
                    LouseRed => 30f,
                    SlaverBlue => 30f,
                    SlaverRed => 30f,
                    SpikeSlimeLarge => 30f,
                    SpikeSlimeMedium => 30f,
                    SpikeSlimeSmall => 30f,

                    // Exordium Elites

                    GremlinNob => 30f,
                    Lagavulin => 20f,
                    Sentry => 10f,

                    // Exordium Bosses

                    Guardian => 40f,
                    Hexaghost => 0f,
                    SlimeBoss => 30f,
                    
                    // City Enemies
                    
                    Byrd => 0f,
                    Centurion => 30f,
                    Mugger => 30f,
                    Mystic => 30f,
                    Chosen => 30f,
                    ShelledParasite => 30f,
                    SnakePlant => 30f,
                    SphericGuardian => 30f,
                    Pointy => 30f,
                    Romeo => 30f,
                    Bear => 30f,
                    
                    // City Elites
                    
                    Taskmaster => 30f,
                    BookOfStabbing => 30f,
                    GremlinLeader => 30f,
                    
                    // City Bosses
                    
                    TorchHead => 30f,
                    Collector => 30f,
                    Champ => 30f,
                    BronzeAutomaton => 30f,
                    BronzeOrb => 30f,
                    
                    // Beyond Enemies
                    
                    Darkling => 30f,
                    Exploder => 30f,
                    Maw => 30f,
                    OrbWalker => 30f,
                    Repulsor => 30f,
                    Spiker => 30f,
                    SpireGrowth => 30f,
                    Transient => 30f,
                    WrithingMass => 30f,
                    
                    // Beyond Elites
                    
                    GiantHead => 30f,
                    Nemesis => 30f,
                    Reptomancer => 30f,
                    SnakeDagger => 30f,
                    
                    // Beyond Bosses
                    
                    AwakenedOne => 30f,
                    Donu => 30f,
                    Deca => 30f,
                    */
                    _ => 0f
                };

                if (offset == 0f)
                    continue;
                
                node.Position += new Vector2(0, offset);
            }
        }
    }
    
    [HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Remove), typeof(PowerModel))]
    public class CurlUpLousePatch
    {
        public static void Prefix(PowerModel? power)
        {
            if (power is CurlUpPower)
            {
                if (power.Owner?.Monster is LouseRed louseRed)
                {
                    louseRed.IsOpen = false;
                }
                else if (power.Owner?.Monster is LouseGreen louseGreen)
                {
                    louseGreen.IsOpen = false;
                }
            }
        }
    }
}