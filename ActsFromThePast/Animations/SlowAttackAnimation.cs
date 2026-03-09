using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;
public static class SlowAttackAnimation
{
    private const float AnimationDuration = 1.0f;
    private const float ActionDuration = 0.5f;
    private const float TargetDistance = 90f;
    
    public static async Task Play(Creature creature)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;
        
        var visuals = creatureNode.Visuals;
        if (visuals == null) return;
        
        var originalPos = visuals.Position;
        
        // Enemies move left (negative X), players move right (positive X)
        var direction = creature.IsPlayer ? 1f : -1f;
        
        var tween = creatureNode.CreateTween();
        
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                float xOffset;
                if (t < 0.5f)
                {
                    // First half: ease in toward target (exp10In)
                    var easedT = Mathf.Pow(t * 2f, 10f);  // Approximate exp10In
                    xOffset = Mathf.Lerp(0f, TargetDistance, easedT);
                }
                else
                {
                    // Second half: fade back to origin
                    var fadeT = (1f - t) * 2f;
                    var easedT = fadeT * fadeT * (3f - 2f * fadeT);  // Smoothstep/fade
                    xOffset = Mathf.Lerp(0f, TargetDistance, easedT);
                }
                
                visuals.Position = new Vector2(originalPos.X + xOffset * direction, originalPos.Y);
            }),
            0f,
            1f,
            AnimationDuration
        ).SetTrans(Tween.TransitionType.Linear);
        
        // Action queue proceeds after 0.5s, animation continues
        await Cmd.Wait(ActionDuration);
    }
}