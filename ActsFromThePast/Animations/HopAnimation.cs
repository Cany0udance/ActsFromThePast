using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class HopAnimation
{
    private static readonly Dictionary<ulong, Vector2> _basePositions = new();

    public static void RegisterBasePosition(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        var visuals = creatureNode?.Visuals;
        if (visuals != null)
            _basePositions[visuals.GetInstanceId()] = visuals.Position;
    }

    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
            return;

        var visuals = creatureNode.Visuals;
        if (visuals == null)
            return;

        var id = visuals.GetInstanceId();
        if (!_basePositions.TryGetValue(id, out var basePos))
        {
            basePos = visuals.Position;
            _basePositions[id] = basePos;
        }

        // Reset to base before starting
        visuals.Position = basePos;

        var hopHeight = 60f;
        var animationDuration = 0.7f;
        var actionDuration = 0.25f;

        var tween = creatureNode.CreateTween();

        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                var yOffset = Mathf.Sin(t * Mathf.Pi) * hopHeight;
                visuals.Position = new Vector2(basePos.X, basePos.Y - yOffset);
            }),
            0f,
            1f,
            animationDuration
        ).SetTrans(Tween.TransitionType.Linear);

        await Cmd.Wait(actionDuration);
    }
}