using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class FastAttackAnimation
{
    private const float AnimationDuration = 0.4f;
    private const float ActionDuration = 0.25f;
    private const float TargetDistance = 90f;
    
    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;
        
        var visuals = creatureNode.Visuals;
        if (visuals == null) return;
        
        var originalPos = creatureNode.Position;
        var direction = creature.IsPlayer ? 1f : -1f;
        
        var tween = creatureNode.CreateTween();
        
        tween.TweenMethod(
            Callable.From<float>(timer =>
            {
                float xOffset;
                if (timer < 0f)
                {
                    xOffset = 0f;
                }
                else
                {
                    // fade: t * t * (3 - 2t)
                    var t = timer / 1f * 2f;  // Matches original's (timer / 1.0F * 2.0F)
                    var easedT = t * t * (3f - 2f * t);
                    xOffset = Mathf.Lerp(0f, TargetDistance, easedT);
                }
                
                creatureNode.Position = new Vector2(originalPos.X + xOffset * direction, originalPos.Y);
            }),
            AnimationDuration,  // Start at 0.4
            0f,                 // Count down to 0
            AnimationDuration
        ).SetTrans(Tween.TransitionType.Linear);
        
        await Cmd.Wait(ActionDuration);
    }
}