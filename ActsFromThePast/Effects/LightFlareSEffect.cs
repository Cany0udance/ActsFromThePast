using Godot;

namespace ActsFromThePast;

public partial class LightFlareSEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private static readonly string[] FlareRegions =
    {
        "env/lightFlare1", "env/lightFlare2"
    };
    
    private Sprite2D _sprite;
    private float _rotation;
    private float _scale;
    private Color _color;
    private bool _renderGreen;
    
    public static LightFlareSEffect Create(float x, float y, bool renderGreen)
    {
        var effect = new LightFlareSEffect();
        effect._renderGreen = renderGreen;
        effect.SetupAt(x, y);
        return effect;
    }
    
    private void SetupAt(float x, float y)
    {
        Setup();
        
        StartingDuration = (float)GD.RandRange(2.0, 3.0);
        Duration = StartingDuration;
        
        var regionName = FlareRegions[Random.Shared.Next(FlareRegions.Length)];
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
        
        // Additive blending for light glow
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        _sprite.Material = material;
        
        AddChild(_sprite);
        
        // Convert StS1 coordinates to centered Godot coordinates
        float halfWidth = 960f;
        float halfHeight = 568f;
        float localX = x - halfWidth - 23f;
        float localY = halfHeight - y;
        
        Position = new Vector2(localX, localY);
        
        _scale = (float)GD.RandRange(3.0, 3.5);
        _rotation = (float)GD.RandRange(0, 360);
        
        if (!_renderGreen)
        {
            _color = new Color(
                (float)GD.RandRange(0.6, 1.0),
                (float)GD.RandRange(0.4, 0.7),
                (float)GD.RandRange(0.2, 0.3),
                0.01f
            );
        }
        else
        {
            _color = new Color(
                (float)GD.RandRange(0.1, 0.3),
                (float)GD.RandRange(0.5, 0.9),
                (float)GD.RandRange(0.1, 0.3),
                0.01f
            );
        }
        
        UpdateSprite();
    }
    
    protected override void Initialize()
    {
        // Initialization happens in SetupAt
    }
    
    protected override void Update(float delta)
    {
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        // Fade in during first second, fade out after
        float elapsed = StartingDuration - Duration;
        if (elapsed < 1.0f)
        {
            _color.A = Fade(1f - (Duration / StartingDuration)) * 0.2f;
        }
        else
        {
            _color.A = Fade(Duration / StartingDuration) * 0.2f;
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