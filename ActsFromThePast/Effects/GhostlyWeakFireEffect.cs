using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace ActsFromThePast;

public partial class GhostlyWeakFireEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private static readonly string[] FireRegions =
    {
        "env/fire1", "env/fire2", "env/fire3"
    };
    
    private Sprite2D _sprite;
    private float _x;
    private float _y;
    private float _vX;
    private float _vY;
    private float _scale;
    private Color _color;
    
    public static GhostlyWeakFireEffect Create(float x, float y)
    {
        var effect = new GhostlyWeakFireEffect();
        effect._x = x + (float)GD.RandRange(-2.0, 2.0);
        effect._y = y + (float)GD.RandRange(-2.0, 2.0);
        effect._vX = (float)GD.RandRange(-2.0, 2.0);
        effect._vY = (float)GD.RandRange(-80.0, 0.0); // Y inverted in Godot
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = 1.0f;
        StartingDuration = 1.0f;
        
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
        
        _scale = (float)GD.RandRange(2.0, 3.0);
        
        // Sky blue color
        _color = new Color(0.53f, 0.81f, 0.92f, 0.0f);
        
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        _x += _vX * delta;
        _y += _vY * delta;
        
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        _color.A = Duration / 2f;
        
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (_sprite == null) return;
        
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
    }
}