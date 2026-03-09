using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class RiseAnimation
{
    private const float Duration = 0.3f;

    public static async Task Play(Creature creature, float riseDistance)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;

        var visuals = creatureNode.Visuals;
        if (visuals == null) return;

        var originalPos = visuals.Position;

        var tween = creatureNode.CreateTween();
        tween.TweenProperty(visuals, "position:y",
                originalPos.Y - riseDistance, Duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);

        await creatureNode.ToSignal(tween, Tween.SignalName.Finished);
    }
}