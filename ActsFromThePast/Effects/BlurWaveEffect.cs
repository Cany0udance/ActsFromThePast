using Godot;

namespace ActsFromThePast;

public partial class BlurWaveEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 2f;
    private const float SpeedTarget = 2000f;
    private const float Flipper = 270f;
    
    private Sprite2D _sprite;
    private float _rotation;
    private float _scale;
    private float _speed;
    private float _speedStart;
    private float _stallTimer;
    private float _duration = EffectDuration;
    private Color _color;
    private ShockWaveType _type;
    
    public static BlurWaveEffect Create(Vector2 position, Color color, ShockWaveType type, float speed, float duration = EffectDuration)
    {
        var effect = new BlurWaveEffect();
        effect.Position = position;
        effect._speedStart = speed;
        effect._speed = speed;
        effect._type = type;
        effect._duration = duration;
        
        effect._stallTimer = (float)GD.RandRange(0f, 0.3f);
        effect._rotation = (float)GD.RandRange(0f, 360f);
        effect._scale = (float)GD.RandRange(0.5f, 0.9f);
        
        effect._color = color;
        if (type != ShockWaveType.Chaotic)
        {
            effect._color.G -= (float)GD.RandRange(0f, 0.1f);
            effect._color.B -= (float)GD.RandRange(0f, 0.2f);
        }
        effect._color.A = 0f;
        
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/blurWave");
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
        _sprite.Visible = true;
        AddChild(_sprite);
        Duration = _duration;
        StartingDuration = _duration;
        
        _sprite.ZIndex = GD.Randf() > 0.5f ? -1 : 1;
        
        if (_type != ShockWaveType.Normal)
        {
            _sprite.Material = CreateAdditiveMaterial();
        }
        
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        _stallTimer -= delta;
        
        if (_stallTimer >= 0f)
            return;
        
        Duration -= delta;
        
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        // Move outward based on rotation angle
        float radians = Mathf.DegToRad(_rotation);
        float dx = Mathf.Cos(radians) * _speed * delta;
        float dy = Mathf.Sin(radians) * _speed * delta;
        Position += new Vector2(dx, dy);
        
        // Interpolate speed from start to target
        float t = 1f - Duration / StartingDuration;
        _speed = Lerp(_speedStart, SpeedTarget, Smootherstep(t));
        
        // Grow scale multiplicatively
        _scale *= 1f + delta * 2f;
        
        // Alpha: fade in during first 0.5s, hold at 0.7, fade out during last 0.5s
        if (Duration > 1.5f)
        {
            float fadeT = (StartingDuration - Duration) * 2f;
            _color.A = Lerp(0f, 0.7f, Smootherstep(fadeT));
        }
        else if (Duration < 0.5f)
        {
            float fadeT = Duration * 2f;
            _color.A = Lerp(0f, 0.7f, Smootherstep(fadeT));
        }
        else
        {
            _color.A = 0.7f;
        }
        
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        float scaleJitterRange = _type == ShockWaveType.Chaotic ? 0.1f : 0.08f;
        float rotationJitterRange = _type == ShockWaveType.Chaotic ? 30f : 3f;
        
        float scaleJitterX = (float)GD.RandRange(-scaleJitterRange, scaleJitterRange);
        float scaleJitterY = (float)GD.RandRange(-scaleJitterRange, scaleJitterRange);
        float rotationJitter = (float)GD.RandRange(-rotationJitterRange, rotationJitterRange);
        
        _sprite.Scale = new Vector2(_scale + scaleJitterX, _scale + scaleJitterY);
        _sprite.RotationDegrees = _rotation + Flipper + rotationJitter;
        _sprite.Modulate = _color;
    }
    
    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}