using System.Reflection;
using ActsFromThePast.Acts.Exordium.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class DeadAdventurerPatches
{
    // TODO All room event rewards BREAK on save & load!! fix plz
    private static readonly HashSet<Type> DeadAdventurerEncounters = new()
    {
        typeof(DeadAdventurerSentries),
        typeof(DeadAdventurerGremlinNob),
        typeof(DeadAdventurerLagavulin),
    };

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public class VisualsPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        if (__instance.Mode != CombatRoomMode.VisualOnly)
            return;

        var visualsField = typeof(NCombatRoom).GetField(
            "_visuals",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var visuals = visualsField?.GetValue(__instance)
            as ICombatRoomVisuals;

        if (visuals?.Encounter == null) return;

        if (!DeadAdventurerEncounters.Contains(
                visuals.Encounter.GetType()))
            return;

        // Hide enemies until search fails
        var enemyContainer = __instance
            .GetNodeOrNull<Control>("%EnemyContainer");
        if (enemyContainer != null)
            enemyContainer.Visible = false;

        // Add event background image
        var texture = GD.Load<Texture2D>(
            "res://images/event_extras/dead_adventurer.png");
        if (texture == null) return;

        var overlay = new TextureRect();
        overlay.Name = "EventImageOverlay";
        overlay.Texture = texture;
        overlay.StretchMode = TextureRect.StretchModeEnum.Scale;
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.OffsetTop = 40f;
        overlay.ZIndex = -11;

        __instance.AddChild(overlay);

        // Mirror SceneContainer's shake onto the overlay
        var sceneContainer = __instance
            .GetNodeOrNull<Control>("%CombatSceneContainer");
        if (sceneContainer != null)
        {
            var basePos = sceneContainer.Position;
            sceneContainer.Connect(
                "item_rect_changed",
                Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(overlay) &&
                        GodotObject.IsInstanceValid(sceneContainer))
                    {
                        var delta = sceneContainer.Position - basePos;
                        overlay.OffsetTop = 40f + delta.Y;
                        overlay.OffsetLeft = delta.X;
                    }
                }));
        }
    }
}

    /// <summary>
    /// Reveal enemies when Dead Adventurer search fails.
    /// Call from DeadAdventurer.TriggerCombat().
    /// </summary>
    public static void RevealEnemies()
    {
        if (NEventRoom.Instance?.Layout
            is not NCombatEventLayout layout)
            return;

        var combatRoom = layout.EmbeddedCombatRoom;
        if (combatRoom == null) return;

        var enemyContainer = combatRoom
            .GetNodeOrNull<Control>("%EnemyContainer");
        if (enemyContainer != null)
            enemyContainer.Visible = true;
    }
    
    [HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
    public class DeadAdventurerRewardsPatch
    {
        public static void Postfix(RewardsSet __result, AbstractRoom room)
        {
            if (room is not CombatRoom combatRoom)
                return;

            if (!DeadAdventurerEncounters.Contains(
                    combatRoom.Encounter.GetType()))
                return;

            var extraRewards = combatRoom.ExtraRewards.Values
                .SelectMany(list => list)
                .ToHashSet();

            // Remove auto-generated gold, card, and relic rewards.
            // Keep potions (from rolls and relics like White Beast Statue)
            // and keep our explicit extra rewards.
            __result.Rewards.RemoveAll(r =>
                !extraRewards.Contains(r) &&
                r is GoldReward or CardReward or RelicReward);
        }
    }
}