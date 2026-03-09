using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public partial class GhostIgniteEffect : NSts1Effect
{
    private const int Count = 25;
    
    private float _x;
    private float _y;
    
    public static GhostIgniteEffect Create(float x, float y)
    {
        var effect = new GhostIgniteEffect();
        effect._x = x;
        effect._y = y;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = 0.1f; // Just needs to run once
        StartingDuration = Duration;
    }
    
    protected override void Update(float delta)
    {
        // Spawn all particles immediately
        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer != null)
        {
            for (int i = 0; i < Count; i++)
            {
                var burst = FireBurstParticleEffect.Create(_x, _y);
                vfxContainer.AddChild(burst);
                
                var flare = LightFlareParticleEffect.Create(_x, _y, new Color(0.5f, 1f, 0f, 1f)); // Chartreuse
                vfxContainer.AddChild(flare);
            }
        }
        
        IsDone = true;
    }
}