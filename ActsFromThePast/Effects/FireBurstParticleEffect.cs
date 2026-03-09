using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace ActsFromThePast;

public partial class FireBurstParticleEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private static readonly string[] FireRegions =
    {
        "env/fire1", "env/fire2", "env/fire3"
    };
    
    private const float Gravity = 180f;
    
    private Sprite2D _sprite;
    private float _x;
    private float _y;
    private float _vX;
    private float _vY;
    private float _floor;
    private float _scale;
    private float _rotation;
    private Color _color;
    
    public static FireBurstParticleEffect Create(float x, float y)
    {
        var effect = new FireBurstParticleEffect();
        effect._x = x;
        effect._y = y;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = (float)GD.RandRange(0.5, 1.0);
        StartingDuration = Duration;
        
        var regionName = FireRegions[Random.Shared.Next(FireRegions.Length)];
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
        
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        _sprite.Material = material;
        
        AddChild(_sprite);
        
        // Green-ish color (chartreuse variant)
        _color = new Color(
            (float)GD.RandRange(0.1, 0.3),
            (float)GD.RandRange(0.8, 1.0),
            (float)GD.RandRange(0.1, 0.3),
            0.0f
        );
        
        _rotation = (float)GD.RandRange(-10.0, 10.0);
        _scale = (float)GD.RandRange(2.0, 4.0);
        _vX = (float)GD.RandRange(-900.0, 900.0);
        _vY = (float)GD.RandRange(-500.0, 0.0); // Y inverted - negative is up
        _floor = _y + (float)GD.RandRange(100.0, 250.0); // Floor is below start position in Godot
        
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        // Apply gravity (positive because down is positive Y in Godot)
        _vY += Gravity / _scale * delta;
        
        // Weird sine movement from original
        _x += _vX * delta * Mathf.Sin(delta);
        _y += _vY * delta;
        
        // Shrink
        if (_scale > 0.3f)
        {
            _scale -= delta * 2f;
        }
        
        // Bounce off floor
        if (_y > _floor)
        {
            _vY = -_vY * 0.75f;
            _y = _floor - 0.1f;
            _vX *= 1.1f;
        }
        
        // Fade in then out
        float progress = 1f - Duration / StartingDuration;
        if (progress < 0.1f)
        {
            // Fade in
            _color.A = EaseOut(progress * 10f);
        }
        else
        {
            // Fade out (pow2Out)
            float t = Duration / StartingDuration;
            _color.A = t * t;
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
    }
}