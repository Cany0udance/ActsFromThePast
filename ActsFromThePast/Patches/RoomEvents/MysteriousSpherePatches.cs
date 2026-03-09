using System.Reflection;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class MysteriousSpherePatches
{
    private static readonly HashSet<Type> SphereEncounters = new()
    {
        typeof(TwoOrbWalkersEvent)
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
            if (!SphereEncounters.Contains(
                    visuals.Encounter.GetType()))
                return;

            // Add background image
            var bgTexture = GD.Load<Texture2D>(
                "res://images/event_extras/bgSphere.png");
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

            // Mirror screen shake
            var sceneContainer = __instance
                .GetNodeOrNull<Control>("%CombatSceneContainer");
            if (sceneContainer != null)
            {
                var basePos = sceneContainer.Position;
                var bgNode = __instance
                    .GetNodeOrNull<TextureRect>("EventBgOverlay");
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
                    }));
            }
        }
    }

    [HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
    public class RewardsPatch
    {
        public static void Postfix(RewardsSet __result, AbstractRoom room)
        {
            if (room is not CombatRoom combatRoom)
                return;
            if (!SphereEncounters.Contains(
                    combatRoom.Encounter.GetType()))
                return;

            var extraRewards = combatRoom.ExtraRewards.Values
                .SelectMany(list => list)
                .ToHashSet();

            __result.Rewards.RemoveAll(r =>
                !extraRewards.Contains(r) &&
                r is GoldReward or RelicReward);
        }
    }
    
    public static void SwapToOpenSphere()
    {
        if (NEventRoom.Instance?.Layout
            is not NCombatEventLayout layout)
            return;
        var combatRoom = layout.EmbeddedCombatRoom;
        if (combatRoom == null) return;
        var bgNode = combatRoom.GetNodeOrNull<TextureRect>("EventBgOverlay");
        if (bgNode == null) return;

        var openTexture = GD.Load<Texture2D>(
            "res://images/event_extras/bgSphereOpen.png");
        if (openTexture != null)
            bgNode.Texture = openTexture;
    }
}