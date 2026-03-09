using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class StaggerAnimation
{
    private const string LOG_TAG = "[ActsFromThePast]";
    private const float StaggerDuration = 0.3f;
    private const float StaggerDistance = 20f;
    
    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null)
        {
            return;
        }
    
        var originalPos = creatureNode.Position;
    
        // Enemies stagger right (positive X), players stagger left (negative X)
        var direction = creature.IsPlayer ? -1f : 1f;
    
        var tween = creatureNode.CreateTween();
    
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                // pow2 is ease-in: t * t
                var easedT = t * t;
                // Interpolate from StaggerDistance to 0
                var xOffset = Mathf.Lerp(StaggerDistance, 0f, easedT) * direction;
                creatureNode.Position = new Vector2(originalPos.X + xOffset, originalPos.Y);
            }),
            0f,
            1f,
            StaggerDuration
        ).SetTrans(Tween.TransitionType.Linear);
    
        await creatureNode.ToSignal(tween, Tween.SignalName.Finished);
    
        creatureNode.Position = originalPos;
    }
}