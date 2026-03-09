using Godot;

namespace ActsFromThePast;

public partial class SmokeBlurEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    private Sprite2D _sprite;
    private float _vDrift;
    private float _angularVelocity;
    private float _targetScale;
    private float _rotation;
    private Color _color;
    private bool _useLargeImage;

    public static SmokeBlurEffect Create(Vector2 position)
    {
        var effect = new SmokeBlurEffect();
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        _useLargeImage = GD.Randf() > 0.5f;
        
        if (_useLargeImage)
        {
            Duration = (float)GD.RandRange(2.0f, 2.5f);
            _targetScale = (float)GD.RandRange(0.8f, 2.2f);
        }
        else
        {
            Duration = (float)GD.RandRange(2.0f, 2.5f);
            _targetScale = (float)GD.RandRange(0.8f, 1.2f);
        }
        
        StartingDuration = Duration;

        var regionName = _useLargeImage ? "exhaust/bigBlur" : "exhaust/smallBlur";
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
        _sprite.Visible = true;
        AddChild(_sprite);

        // Offset positions - Y inverted for Godot's coordinate system
        float offsetX = (float)GD.RandRange(-180f, 150f);
        float offsetY = (float)GD.RandRange(-150f, 240f); // Inverted range
        Position += new Vector2(offsetX, offsetY);

        _color = new Color(
            (float)GD.RandRange(0.5f, 0.6f),
            0f,
            0.2f,
            1f
        );
        _color.G = _color.R + (float)GD.RandRange(0f, 0.2f);

        _sprite.Scale = new Vector2(0.01f, 0.01f);
        _rotation = (float)GD.RandRange(0f, 360f);
        _angularVelocity = (float)GD.RandRange(-250f, 250f);
        _vDrift = (float)GD.RandRange(1f, 5f);

        UpdateSprite();
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        float jitterX = (float)GD.RandRange(-2f, 2f);
        float jitterY = (float)GD.RandRange(-2f, 2f);
        
        // Drift up and right (positive X, negative Y in Godot)
        Position += new Vector2(jitterX + _vDrift, jitterY - _vDrift);

        _rotation += _angularVelocity * delta;

        float t = 1f - Duration / StartingDuration;
        float currentScale = Lerp(0.01f, _targetScale, Exp10Out(t));
        _sprite.Scale = new Vector2(currentScale, currentScale);

        if (Duration < 0.33f)
        {
            _color.A = Duration * 3f;
        }

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        _sprite.RotationDegrees = _rotation;
        _sprite.Modulate = _color;
    }

    private static float Exp10Out(float t)
    {
        return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    }
}