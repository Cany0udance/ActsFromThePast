using ActsFromThePast.Acts.TheBeyond.Enemies;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Patches.Creatures;

public class AnimationPatches
{
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
public class StaggerAnimationPatch
{
    public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
    {
        // Handle mix duration overrides first (before the Hit-only gate)
        if (creature?.Monster is Lagavulin lag && lag.IsAwake)
        {
            float mix = triggerName switch
            {
                "Hit" => 0.25f,
                "Attack" => 0.25f,
                "Debuff" => 0.25f,
                _ => 0f
            };

            if (mix > 0f)
            {
                __result = PlayHitWithMix(creature, triggerName, "Idle_2", mix);
                return false;
            }
        }

        if (triggerName != "Hit")
            return true;

        if (creature?.Monster is AwakenedOne awOne)
        {
            if (awOne.Respawns >= 1 && triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle_2", 0.2f);
                return false;
            }
            if (awOne.Respawns == 0)
            {
                float mix = triggerName switch
                {
                    "Hit" => 0.3f,
                    "Slash" => 0.2f,
                    _ => 0f
                };

                if (mix > 0f)
                {
                    __result = PlayHitWithMix(creature, triggerName, "Idle_1", mix);
                    return false;
                }
            }
        }

        if (creature?.Monster is ShelledParasite)
        {
            __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
            return false;
        }
        
        if (creature?.Monster is SphericGuardian)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
            if (triggerName == "Attack")
            {
                __result = PlayAnimWithMix(creature, "Attack", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is Chosen)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
            if (triggerName == "Attack")
            {
                __result = PlayHitWithMix(creature, "Attack", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is Sentry)
        {
            var (animName, mix) = triggerName switch
            {
                "Attack" => ("attack", 0.1f),
                "spaz1" => ("spaz1", 0.1f),
                "spaz2" => ("spaz2", 0.1f),
                "spaz3" => ("spaz3", 0.1f),
                "Hit" => ("hit", 0.1f),
                _ => (null, 0f)
            };

            if (animName != null)
            {
                __result = PlayAnimWithMixBothWays(creature, animName, "idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is Bear)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is Romeo)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is Pointy)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is BookOfStabbing)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is Centurion)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is Mystic)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is GremlinLeader)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is SnakePlant)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is Snecko)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is Deca)
        {
            float mix = triggerName switch
            {
                "Hit" => 0.1f,
                "Beam" => 0.1f,
                _ => 0f
            };

            if (mix > 0f)
            {
                __result = PlayHitWithMix(creature, triggerName, "Idle", mix);
                return false;
            }
        }
        
        if (creature?.Monster is Donu)
        {
            float mix = triggerName switch
            {
                "Hit" => 0.1f,
                "Beam" => 0.1f,
                _ => 0f
            };

            if (mix > 0f)
            {
                __result = PlayHitWithMix(creature, triggerName, "Idle", mix);
                return false;
            }
        }
        
        if (creature?.Monster is Nemesis)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is Reptomancer)
        {
            if (triggerName is "Strike" or "Summon" or "Hit")
            {
                __result = PlayAnimWithMixBothWays(creature, triggerName, "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is SpireGrowth)
        {
            if (triggerName == "Hit")
            {
                __result = PlayAnimWithMixBothWays(creature, triggerName, "Idle", 0.2f);
                return false;
            }
        }
        
        if (creature?.Monster is TimeEater)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }
        
        if (creature?.Monster is SpireGrowth)
        {
            if (triggerName == "Hit")
            {
                __result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
                return false;
            }
        }

        var useStagger = creature?.Monster switch
        {
            // Exordium Enemies
            AcidSlimeLarge => false,
            AcidSlimeMedium => false,
            AcidSlimeSmall => false,
            Cultist => true,
            FungiBeast => false,
            GremlinFat => true,
            GremlinMad => true,
            GremlinShield => true,
            GremlinSneaky => true,
            GremlinWizard => true,
            JawWorm => true,
            Looter => true,
            LouseGreen => true,
            LouseRed => true,
            SlaverBlue => true,
            SlaverRed => true,
            SpikeSlimeLarge => false,
            SpikeSlimeMedium => false,
            SpikeSlimeSmall => false,

            // Exordium Elites
            GremlinNob => true,
            Lagavulin => false,
            Sentry => false,

            // Exordium Bosses
            Guardian => true,
            Hexaghost => false,
            SlimeBoss => false,

            // City Enemies
            Byrd => true,
            Chosen => true,
            Centurion => false,
            Mugger => true,
            Mystic => false,
            ShelledParasite => false,
            SnakePlant => false,
            SphericGuardian => false,
            Pointy => false,
            Romeo => false,
            Bear => false,

            // City Elites
            Taskmaster => true,
            BookOfStabbing => false,
            GremlinLeader => false,

            // City Bosses
            TorchHead => true,
            Collector => true,
            Champ => false,
            BronzeAutomaton => true,
            BronzeOrb => true,

            // Beyond Enemies
            Darkling => false,
            Exploder => true,
            Maw => true,
            Repulsor => true,
            Spiker => false,
            SpireGrowth => false,
            Transient => false,
            OrbWalker => false,
            WrithingMass => false,

            // Beyond Elites
            GiantHead => true,
            Nemesis => false,
            Reptomancer => false,
            SnakeDagger => false,

            // Beyond Bosses
            AwakenedOne => false,
            Donu => false,
            Deca => false,

            _ => false
        };

        if (!useStagger)
            return true;

        _ = StaggerAnimation.Play(creature);
        __result = Task.CompletedTask;
        return false;
    }

    /*
    
    private static async Task PlayAwakenedPhase2Hit(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            animState.SetAnimation("Hit", false, 0);
            var queued = animState.AddAnimation("Idle_2", 0.0f, true, 0);
            queued?.SetMixDuration(0.2f);
        }
    }
    
    */

    private static async Task PlayHitWithMix(Creature creature, string hitAnim, string idleAnim, float mixDuration)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            animState.SetAnimation(hitAnim, false, 0);
            var queued = animState.AddAnimation(idleAnim, 0.0f, true, 0);
            queued?.SetMixDuration(mixDuration);
        }
    }
    
    private static async Task PlayAnimWithMix(Creature creature, string animName, float mixDuration)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var entry = animState.SetAnimation(animName, false, 0);
            entry?.SetMixDuration(mixDuration);
        }
    }
    
    private static async Task PlayAnimWithMixBothWays(Creature creature, string animName, string idleAnim, float mixDuration)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var entry = animState.SetAnimation(animName, false, 0);
            entry?.SetMixDuration(mixDuration);
            var queued = animState.AddAnimation(idleAnim, 0.0f, true, 0);
            queued?.SetMixDuration(mixDuration);
        }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.GetCurrentAnimationTimeRemaining))]
public static class DeathAnimTimePatch
{
    private static readonly HashSet<NCreature> _dyingCreatures = new();

    public static void MarkAsDying(NCreature creature) => _dyingCreatures.Add(creature);

    public static bool Prefix(NCreature __instance, ref float __result)
    {
        if (__instance.Entity.Monster == null)
            return true;

        if (!_dyingCreatures.Contains(__instance))
            return true;
        
        _dyingCreatures.Remove(__instance);
        __result = 0f;
        return false;
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class DeathAnimStartPatch
{
    public static void Prefix(NCreature __instance)
    {
        var shouldOverride = __instance.Entity.Monster switch
        {
            // Exordium Enemies
            
            AcidSlimeLarge => true,
            AcidSlimeMedium => true,
            AcidSlimeSmall => true,
            Cultist => true,
            FungiBeast => true,
            GremlinFat => true,
            GremlinMad => true,
            GremlinShield => true,
            GremlinSneaky => true,
            GremlinWizard => true,
            JawWorm => true,
            Looter => true,
            LouseGreen => true,
            LouseRed => true,
            SlaverBlue => true,
            SlaverRed => true,
            SpikeSlimeLarge => true,
            SpikeSlimeMedium => true,
            SpikeSlimeSmall => true,
            
            // Exordium Elites
            
            GremlinNob => true,
            Lagavulin => true,
            Sentry => true,
            
            // Exordium Bosses
            
            Guardian => true,
            Hexaghost => false,
            SlimeBoss => true,
            
            // City Enemies
            
            Byrd => true,
            Centurion => true,
            Mugger => true,
            Mystic => true,
            Chosen => true,
            ShelledParasite => true,
            SnakePlant => true,
            SphericGuardian => true,
            Pointy => true,
            Romeo => true,
            Bear => true,
                
            // City Elites
                
            Taskmaster => true,
            BookOfStabbing => true, // TODO investigate why stab book is not affected by this
            GremlinLeader => true,
                
            // City Bosses
            
            TorchHead => true,
            Collector => true,
            Champ => true,
            BronzeAutomaton => true,
            BronzeOrb => false,
            
            // Beyond Enemies
            
            Darkling => true, // TODO investigate why darklings are not affected by this
            Exploder => true,
            Maw => true,
            OrbWalker => true,
            Repulsor => true,
            Spiker => true,
            SpireGrowth => true,
            Transient => true,
            WrithingMass => true,
            
            // Beyond Elites
            
            GiantHead => true,
            Nemesis => true,
            Reptomancer => true, // TODO investigate why Reptoman is not affected by this
            SnakeDagger => true,
            
            // Beyond Bosses
            
            AwakenedOne => true,
            Donu => true,
            Deca => true,
            
            _ => false
        };
        if (!shouldOverride)
            return;

        DeathAnimTimePatch.MarkAsDying(__instance);
    }
}
}