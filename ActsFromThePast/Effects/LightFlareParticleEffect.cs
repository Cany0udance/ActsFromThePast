using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace ActsFromThePast;

public partial class LightFlareParticleEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const string BlurRegion = "combat/blurDot";
    
    private Sprite2D _sprite;
    private Sprite2D _glowSprite;
    private float _x;
    private float _y;
    private float _speed;
    private float _speedStart;
    private float _speedTarget;
    private float _waveIntensity;
    private float _waveSpeed;
    private float _rotation;
    private float _scale;
    private Color _color;
    
    public static LightFlareParticleEffect Create(float x, float y, Color color)
    {
        var effect = new LightFlareParticleEffect();
        effect._x = x;
        effect._y = y;
        effect._color = color;
        effect._color.A = 0f;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = (float)GD.RandRange(0.5, 1.1);
        StartingDuration = Duration;
        
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, BlurRegion);
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }
        
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        
        // Glow sprite (rendered larger, more transparent)
        _glowSprite = new Sprite2D();
        _glowSprite.Texture = textureRegion.Value.Texture;
        _glowSprite.RegionEnabled = true;
        _glowSprite.RegionRect = textureRegion.Value.Region;
        _glowSprite.Centered = true;
        _glowSprite.Material = material;
        _glowSprite.ZIndex = -1;
        AddChild(_glowSprite);
        
        // Main sprite
        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        _sprite.Material = material;
        AddChild(_sprite);
        
        _speed = (float)GD.RandRange(200.0, 300.0);
        _speedStart = _speed;
        _speedTarget = (float)GD.RandRange(0.1, 0.5);
        _rotation = (float)GD.RandRange(0.0, 360.0);
        _waveIntensity = (float)GD.RandRange(5.0, 10.0);
        _waveSpeed = (float)GD.RandRange(-20.0, 20.0);
        _scale = (float)GD.RandRange(0.2, 1.0);
        
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        // Move in direction of rotation
        float radians = Mathf.DegToRad(_rotation);
        float dx = Mathf.Cos(radians) * _speed * delta;
        float dy = Mathf.Sin(radians) * _speed * delta;
        
        _x += dx;
        _y += dy;
        
        // Interpolate speed (pow2OutInverse approximation)
        float progress = 1f - Duration / StartingDuration;
        _speed = Lerp(_speedStart, _speedTarget, Mathf.Sqrt(progress));
        
        // Wave rotation
        _rotation += Mathf.Cos(Duration * _waveSpeed) * _waveIntensity;
        
        // Fade
        if (Duration < 0.5f)
        {
            _color.A = EaseOut(Duration * 2f);
        }
        else
        {
            _color.A = 1f;
        }
        
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (_sprite == null) return;
        
        _sprite.RotationDegrees = _rotation;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
        
        _glowSprite.RotationDegrees = _rotation;
        _glowSprite.Scale = new Vector2(_scale * 4f, _scale * 4f);
        _glowSprite.Modulate = new Color(_color.R, _color.G, _color.B, _color.A / 4f);
    }
}