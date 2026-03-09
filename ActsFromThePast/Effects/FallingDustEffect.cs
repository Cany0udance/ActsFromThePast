using Godot;

namespace ActsFromThePast;

public partial class FallingDustEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private Sprite2D _sprite;
    private float _x;
    private float _y;
    private float _vY;
    private float _vX;
    private float _vYAccel;
    private float _aV;
    private float _startingAlpha;
    private float _scale;
    private float _rotation;
    
    public static FallingDustEffect Create(float x, float y)
    {
        var effect = new FallingDustEffect();
        effect._x = x + (float)GD.RandRange(-6.0, 6.0);
        effect._y = y;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        // Random vertical offset for depth
        float randY = (float)GD.RandRange(-10.0, 10.0);
        _y += randY;
        
        _vY = -(float)GD.RandRange(0.0, 140.0); // Negative because Godot Y is down
        
        if (GD.Randf() > 0.5f)
            _vX = (float)GD.RandRange(-20.0, 20.0);
        else
            _vX = 0f;
        
        _vYAccel = -(float)GD.RandRange(4.0, 9.0); // Negative for downward acceleration
        
        Duration = (float)GD.RandRange(3.0, 7.0);
        StartingDuration = Duration;
        
        _scale = (float)GD.RandRange(0.5, 0.7);
        _rotation = (float)GD.RandRange(0.0, 360.0);
        _aV = (float)GD.RandRange(-1.0, 1.0);
        
        float c = (float)GD.RandRange(0.1, 0.3);
        float alpha = (float)GD.RandRange(0.8, 0.9);
        EffectColor = new Color(c + 0.1f, c, c, alpha);
        _startingAlpha = alpha;
        
        // Pick a random dust image
        string[] dustRegions = { "env/dust1", "env/dust2", "env/dust3" };
        string regionName = dustRegions[GD.Randi() % dustRegions.Length];
        
        var region = LibGdxAtlas.GetRegion(AtlasPath, regionName);
        if (region == null)
        {
            IsDone = true;
            return;
        }
        
        _sprite = new Sprite2D();
        _sprite.Texture = region.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = region.Value.Region;
        _sprite.Centered = true;
        AddChild(_sprite);
        
        UpdateSprite();
    }
    
    protected override void Update(float delta)
    {
        _rotation += _aV;
        _y -= _vY * delta; // Subtract because we inverted vY
        _x += _vX * delta;
        _vY += _vYAccel * delta;
        _vX *= 0.99f;
        
        if (Duration < 3.0f)
        {
            float t = 1.0f - Duration / 3.0f;
            EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, 
                Lerp(_startingAlpha, 0f, EaseOut(t)));
        }
        
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        _sprite.GlobalPosition = new Vector2(_x, _y);
        _sprite.Modulate = EffectColor;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.RotationDegrees = _rotation;
    }
}