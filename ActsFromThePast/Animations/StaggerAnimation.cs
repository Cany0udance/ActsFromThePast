using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class StaggerAnimation
{
    private const float StaggerDuration = 0.3f;
    private const float StaggerDistance = 20f;

    private static readonly Dictionary<Creature, Vector2> OriginalPositions = new();
    private static readonly Dictionary<Creature, Tween> ActiveTweens = new();

    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
            return;

        // Kill any active tween and reset before capturing position
        if (ActiveTweens.TryGetValue(creature, out var existing) && existing.IsValid())
        {
            existing.Kill();
            if (OriginalPositions.TryGetValue(creature, out var savedPos))
                creatureNode.Position = savedPos;
        }

        // Only store the true origin if we don't already have one mid-stagger
        if (!OriginalPositions.ContainsKey(creature))
            OriginalPositions[creature] = creatureNode.Position;

        var originalPos = OriginalPositions[creature];
        var direction = creature.IsPlayer ? -1f : 1f;

        var tween = creatureNode.CreateTween();
        ActiveTweens[creature] = tween;

        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                var easedT = t * t;
                var xOffset = Mathf.Lerp(StaggerDistance, 0f, easedT) * direction;
                creatureNode.Position = new Vector2(originalPos.X + xOffset, originalPos.Y);
            }),
            0f,
            1f,
            StaggerDuration
        ).SetTrans(Tween.TransitionType.Linear);

        await creatureNode.ToSignal(tween, Tween.SignalName.Finished);

        creatureNode.Position = originalPos;
        OriginalPositions.Remove(creature);
        ActiveTweens.Remove(creature);
    }
    
    public static void Reset()
    {
        foreach (var tween in ActiveTweens.Values)
        {
            if (tween.IsValid())
                tween.Kill();
        }
        ActiveTweens.Clear();
        OriginalPositions.Clear();
    }
}