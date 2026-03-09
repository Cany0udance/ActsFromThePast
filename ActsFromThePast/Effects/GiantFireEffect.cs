using Godot;

namespace ActsFromThePast;

public partial class GiantFireEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 1.5f;
    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;

    private Sprite2D _sprite;
    private float _brightness;
    private float _vX;
    private float _vY;
    private float _delayTimer;
    private float _rotation;
    private float _scale;
    private Color _color;

    public static GiantFireEffect Create()
    {
        var effect = new GiantFireEffect();
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;

        // Random flame texture (1-3)
        int roll = Random.Shared.Next(3);
        string flameName = roll switch
        {
            0 => "combat/flame4",
            1 => "combat/flame5",
            _ => "combat/flame6"
        };

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, flameName);
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
        _sprite.Material = CreateAdditiveMaterial();
        AddChild(_sprite);

        // Random position - starts below screen
        float imgWidth = textureRegion.Value.Region.Size.X;
        float imgHeight = textureRegion.Value.Region.Size.Y;
        float x = (float)(Random.Shared.NextDouble() * ScreenWidth) - imgWidth / 2f;
        float y = ScreenHeight + (float)(Random.Shared.NextDouble() * 200.0 + 200.0) + imgHeight / 2f;
        Position = new Vector2(x, y);

        // Random velocities (vY is negative since Godot Y is down)
        _vX = (float)(Random.Shared.NextDouble() * 140.0 - 70.0);
        _vY = -(float)(Random.Shared.NextDouble() * 1200.0 + 500.0);

        // Random color variation (fire tones)
        _color = new Color(1f, 1f, 1f, 0f);
        float gReduction = (float)(Random.Shared.NextDouble() * 0.5);
        _color.G -= gReduction;
        _color.B -= gReduction - (float)(Random.Shared.NextDouble() * 0.2);

        _rotation = (float)(Random.Shared.NextDouble() * 20.0 - 10.0);
        _scale = (float)(Random.Shared.NextDouble() * 6.5 + 0.5);
        _brightness = (float)(Random.Shared.NextDouble() * 0.4 + 0.2);
        _delayTimer = (float)(Random.Shared.NextDouble() * 0.1);

        // Random flip
        if (Random.Shared.Next(2) == 0)
            _sprite.FlipH = true;

        _sprite.Rotation = Mathf.DegToRad(_rotation);
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
    }

    protected override void Update(float delta)
    {
        if (_delayTimer > 0f)
        {
            _delayTimer -= delta;
            return;
        }

        // Move
        Position += new Vector2(_vX * delta, _vY * delta);

        // Random scale wobble
        _scale *= (float)(Random.Shared.NextDouble() * 0.1 + 0.95);
        _sprite.Scale = new Vector2(_scale, _scale);

        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
        }
        else if (StartingDuration - Duration < 0.75f)
        {
            // Fade in
            float t = (StartingDuration - Duration) / 0.75f;
            _color.A = Lerp(0f, _brightness, Fade(t));
        }
        else if (Duration < 1f)
        {
            // Fade out
            float t = Duration / 1f;
            _color.A = Lerp(0f, _brightness, Fade(t));
        }

        _sprite.Modulate = _color;
    }

    private static float Fade(float t)
    {
        return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}