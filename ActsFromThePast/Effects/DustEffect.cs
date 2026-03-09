using Godot;

namespace ActsFromThePast;

public partial class DustEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private static readonly string[] DustRegions =
    {
        "env/dust1", "env/dust2", "env/dust3", "env/dust4", "env/dust5", "env/dust6"
    };
    
    private Sprite2D _sprite;
    private float _vX;
    private float _vY;
    private float _angularVelocity;
    private float _rotation;
    private float _scale;
    private float _baseAlpha;
    private Color _color;
    
    public static DustEffect Create()
    {
        var effect = new DustEffect();
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        StartingDuration = (float)GD.RandRange(5.0, 14.0);
        Duration = StartingDuration;
        
        var regionName = DustRegions[Random.Shared.Next(DustRegions.Length)];
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
        
        _scale = (float)GD.RandRange(0.1, 0.8);
        
        float halfWidth = 960f;
        
        // StS1: x = random(0, WIDTH)
        float x = (float)GD.RandRange(-halfWidth, halfWidth);
        
        // StS1: y = random(-100, 400) * scale + floorY
        // floorY is roughly 340 in centered Godot coords
        float floorY = 340f;
        float yOffset = (float)GD.RandRange(-100, 400);
        float y = floorY - yOffset;
        
        Position = new Vector2(x, y);
        
        _vX = (float)GD.RandRange(-12, 12);
        _vY = -(float)GD.RandRange(-12, 30);  // Negate for Godot Y-down
        
        _rotation = (float)GD.RandRange(0, 360);
        _angularVelocity = (float)GD.RandRange(-120, 120);
        
        float colorTmp = (float)GD.RandRange(0.1, 0.7);
        _color = new Color(colorTmp, colorTmp, colorTmp, 0f);
        _baseAlpha = 1f - colorTmp;
        
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        _rotation += _angularVelocity * delta;
        Position += new Vector2(_vX * delta, _vY * delta);
        Duration -= delta;
        
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        float halfDuration = StartingDuration / 2f;
        if (Duration > halfDuration)
        {
            // First half: fading in
            // StS1: Interpolation.fade.apply(0, 1, halfDuration - (duration - halfDuration))
            // Simplifies to: elapsed / halfDuration where elapsed = startingDuration - duration
            float elapsed = StartingDuration - Duration;
            _color.A = Fade(elapsed / halfDuration) * _baseAlpha;
        }
        else
        {
            // Second half: fading out
            _color.A = Fade(Duration / halfDuration) * _baseAlpha;
        }
        
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (_sprite == null) return;
        
        _sprite.RotationDegrees = _rotation;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
    }
    
    private static float Fade(float t)
    {
        return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
    }
}