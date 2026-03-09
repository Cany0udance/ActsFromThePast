using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public static class ByrdFallAnimation
{
    private const float Duration = 0.3f;
    public const float SquashDuration = 0.15f;

    public static async Task Play(Creature creature, float fallDistance)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;

        var visuals = creatureNode.Visuals;
        if (visuals == null) return;

        var originalPos = visuals.Position;

        var tween = creatureNode.CreateTween();
        tween.TweenProperty(visuals, "position:y",
                originalPos.Y + fallDistance, Duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);

        await creatureNode.ToSignal(tween, Tween.SignalName.Finished);

        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
    }
}