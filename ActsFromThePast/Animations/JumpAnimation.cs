using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class JumpAnimation
{
    private const float AnimationDuration = 0.7f;
    private const float ActionDuration = 0.25f;
    private const float JumpHeight = 150f; // Approximate peak height

    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
            return;

        var visuals = creatureNode.Visuals;
        if (visuals == null)
            return;

        var originalPos = visuals.Position;

        var tween = creatureNode.CreateTween();

        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                // Parabolic arc: peaks at t=0.5, returns to 0 at t=1
                float animY = 4f * JumpHeight * t * (1f - t);

                // Negative Y because Godot Y-down
                visuals.Position = new Vector2(originalPos.X, originalPos.Y - animY);
            }),
            0f,
            1f,
            AnimationDuration
        ).SetTrans(Tween.TransitionType.Linear);

        await Cmd.Wait(ActionDuration);
    }
}