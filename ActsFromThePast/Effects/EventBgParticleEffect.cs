using Godot;

namespace ActsFromThePast;

public partial class EventBgParticleEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
 
    private Sprite2D _sprite;
    private float _offsetX;
    private float _angularVelocity;
    private float _particleScale;
    private Color _color;
 
    public static EventBgParticleEffect Create(Vector2 center)
    {
        var effect = new EventBgParticleEffect();
        effect.Position = center;
        effect.Setup();
        return effect;
    }
 
    protected override void Initialize()
    {
        Duration = 20f;
        StartingDuration = 20f;
 
        string regionName = GD.Randf() > 0.5f
            ? "eventBgParticle1"
            : "eventBgParticle2";
 
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, regionName);
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }
 
        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        AddChild(_sprite);
 
        // Orbital parameters — radius may need tuning for STS2's viewport
        _offsetX = (float)GD.RandRange(850.0, 950.0);
        _angularVelocity = (float)GD.RandRange(0.01, 7.0) + _offsetX / 300f;
        _particleScale = (float)GD.RandRange(0.3, 3.0) + _offsetX / 900f;
 
        // Sprite sits at orbital radius from parent (screen center)
        _sprite.Position = new Vector2(-_offsetX, 0);
 
        // Random starting angle
        RotationDegrees = (float)GD.RandRange(0, 360);
 
        // Dark teal, starts transparent
        float g = (float)GD.RandRange(0.05, 0.1);
        _color = new Color(0f, g, g, 0f);
 
        _sprite.Scale = new Vector2(_particleScale, _particleScale);
        _sprite.Modulate = _color;
    }
 
    protected override void Update(float delta)
    {
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
 
        // Slow orbit around screen center (CCW to match STS1)
        RotationDegrees -= delta * _angularVelocity;
 
        // Fade: 4s in, 12s full, 4s out
        if (Duration > 16f)
        {
            float t = (Duration - 16f) / 4f;
            _color.A = Lerp(0.3f, 0f, Smootherstep(t));
        }
        else if (Duration < 4f)
        {
            float t = Duration / 4f;
            _color.A = Lerp(0f, 0.3f, Smootherstep(t));
        }
        else
        {
            _color.A = 0.3f;
        }
 
        _sprite.Modulate = _color;
    }
}
