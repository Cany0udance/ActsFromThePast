using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class ShakeAnimation
{
    private const float ShakeSpeed = 150f;
    private const float ShakeThreshold = 8f;
    
    public static async Task Play(Creature creature, float awaitDuration = 1.0f, float? totalDuration = null)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;
        
        var visuals = creatureNode.Visuals;
        if (visuals == null) return;
        
        var originalPos = visuals.Position;
        var actualTotalDuration = totalDuration ?? awaitDuration;
        var elapsed = 0f;
        var shakeToggle = true;
        var animX = 0f;
        
        var tween = creatureNode.CreateTween();
        
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                var delta = t * actualTotalDuration - elapsed;
                elapsed = t * actualTotalDuration;
                
                if (shakeToggle)
                {
                    animX += ShakeSpeed * delta;
                    if (animX > ShakeThreshold)
                    {
                        shakeToggle = false;
                    }
                }
                else
                {
                    animX -= ShakeSpeed * delta;
                    if (animX < -ShakeThreshold)
                    {
                        shakeToggle = true;
                    }
                }
                
                visuals.Position = new Vector2(originalPos.X + animX, originalPos.Y);
            }),
            0f,
            1f,
            actualTotalDuration
        ).SetTrans(Tween.TransitionType.Linear);
        
        tween.Finished += () =>
        {
            visuals.Position = originalPos;
        };
        
        // Only wait for the await duration, animation continues in background
        await Cmd.Wait(awaitDuration);
    }
}