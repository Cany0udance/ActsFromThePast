using Godot;

namespace ActsFromThePast;

public partial class CeilingDustCloudEffect : NSts1Effect
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
    
    public static CeilingDustCloudEffect Create(float x, float y)
    {
        var effect = new CeilingDustCloudEffect();
        effect._x = x + (float)GD.RandRange(-40.0, 40.0);
        effect._y = y;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        var region = LibGdxAtlas.GetRegion(AtlasPath, "env/dustCloud");
        if (region == null)
        {
            IsDone = true;
            return;
        }
        
        float imgWidth = region.Value.Region.Size.X;
        float imgHeight = region.Value.Region.Size.Y;
        
        _x -= imgWidth / 2f;
        _y -= imgHeight / 2f;
        
        float randY = (float)GD.RandRange(-10.0, 10.0);
        _y += randY;
        
        _vY = -(float)GD.RandRange(0.0, 20.0); // Negative for Godot
        _vX = (float)GD.RandRange(-30.0, 30.0);
        _vYAccel = 0f;
        
        Duration = (float)GD.RandRange(3.0, 7.0);
        StartingDuration = Duration;
        
        _scale = (float)GD.RandRange(0.1, 0.7);
        _rotation = (float)GD.RandRange(0.0, 360.0);
        _aV = (float)GD.RandRange(-0.1, 0.1);
        
        float c = (float)GD.RandRange(0.1, 0.3);
        float alpha = (float)GD.RandRange(0.1, 0.2);
        EffectColor = new Color(c + 0.1f, c, c, alpha);
        _startingAlpha = alpha;
        
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
        _y -= _vY * delta;
        _x += _vX * delta;
        _vY += _vYAccel * delta;
        _vX *= 0.99f;
        _scale += delta * 0.2f;
        
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