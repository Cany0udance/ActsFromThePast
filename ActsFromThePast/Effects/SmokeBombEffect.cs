using Godot;

namespace ActsFromThePast;

public partial class SmokeBombEffect : NSts1Effect
{
    private const float EffectDuration = 0.2f;
    private const int ParticleCount = 90;
    
    private bool _spawned;

    public static SmokeBombEffect Create(Vector2 position)
    {
        var effect = new SmokeBombEffect();
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _spawned = false;
    }

    protected override void Update(float delta)
    {
        if (!_spawned)
        {
            _spawned = true;
            ModAudio.Play("general", "attack_whiff_2");
            
            for (int i = 0; i < ParticleCount; i++)
            {
                var blur = SmokeBlurEffect.Create(GlobalPosition);
                GetParent().AddChild(blur);
            }
        }

        Duration -= delta;
        
        if (Duration < 0f)
        {
            ModAudio.Play("general", "appear");
            IsDone = true;
        }
    }
}