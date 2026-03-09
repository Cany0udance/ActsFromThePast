using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class MushroomPatches
{
    private static readonly HashSet<Type> MushroomEncounters = new()
    {
        typeof(ThreeFungiBeastsEvent)
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

            if (!MushroomEncounters.Contains(
                    visuals.Encounter.GetType()))
                return;

            // Hide enemies until player chooses to fight
            var enemyContainer = __instance
                .GetNodeOrNull<Control>("%EnemyContainer");
            if (enemyContainer != null)
                enemyContainer.Visible = false;

            // Add background image
            var bgTexture = GD.Load<Texture2D>(
                "res://images/event_extras/bgShrooms.png");
            if (bgTexture != null)
            {
                var bgOverlay = new TextureRect();
                bgOverlay.Name = "EventBgOverlay";
                bgOverlay.Texture = bgTexture;
                bgOverlay.StretchMode = TextureRect.StretchModeEnum.Scale;
                bgOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
                bgOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                bgOverlay.OffsetTop = 40f;
                bgOverlay.ZIndex = -11;

                __instance.AddChild(bgOverlay);
            }

            // Add foreground image
            var fgTexture = GD.Load<Texture2D>(
                "res://images/event_extras/fgShrooms.png");
            if (fgTexture != null)
            {
                var fgOverlay = new TextureRect();
                fgOverlay.Name = "EventFgOverlay";
                fgOverlay.Texture = fgTexture;
                fgOverlay.StretchMode = TextureRect.StretchModeEnum.Scale;
                fgOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
                fgOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                fgOverlay.OffsetTop = 40f;
                fgOverlay.ZIndex = -6;

                __instance.AddChild(fgOverlay);
            }

            // Mirror screen shake
            var sceneContainer = __instance
                .GetNodeOrNull<Control>("%CombatSceneContainer");
            if (sceneContainer != null)
            {
                var basePos = sceneContainer.Position;
                var bgNode = __instance
                    .GetNodeOrNull<TextureRect>("EventBgOverlay");
                var fgNode = __instance
                    .GetNodeOrNull<TextureRect>("EventFgOverlay");

                sceneContainer.Connect(
                    "item_rect_changed",
                    Callable.From(() =>
                    {
                        if (!GodotObject.IsInstanceValid(sceneContainer))
                            return;

                        var delta = sceneContainer.Position - basePos;

                        if (bgNode != null &&
                            GodotObject.IsInstanceValid(bgNode))
                        {
                            bgNode.OffsetTop = 40f + delta.Y;
                            bgNode.OffsetLeft = delta.X;
                        }

                        if (fgNode != null &&
                            GodotObject.IsInstanceValid(fgNode))
                        {
                            fgNode.OffsetTop = 40f + delta.Y;
                            fgNode.OffsetLeft = delta.X;
                        }
                    }));
            }
        }
    }

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
}