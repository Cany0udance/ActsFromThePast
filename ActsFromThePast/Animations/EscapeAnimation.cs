using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;
public static class EscapeAnimation
{
    private const float EscapeDuration = 3.0f;
    private const float TotalDistance = 400f * 3.0f; // MoveSpeed * Duration = 1200

    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;

        var visuals = creatureNode.Visuals;
        if (visuals == null) return;

        // Flip to face escape direction (right)
        visuals.Scale = new Vector2(-Mathf.Abs(visuals.Scale.X), visuals.Scale.Y);

        var startPos = creatureNode.Position;
        var endPos = startPos + new Vector2(TotalDistance, 0f);

        var tween = creatureNode.CreateTween();
        tween.TweenProperty(creatureNode, "position", endPos, EscapeDuration)
            .SetTrans(Tween.TransitionType.Linear);

        await Cmd.Wait(EscapeDuration);
    }
}