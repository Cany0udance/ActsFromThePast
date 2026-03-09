using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class WobblyLineEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 2.0f;

    private Sprite2D _sprite;
    private float _speed;
    private float _speedStart;
    private float _speedTarget;
    private float _rotation;
    private float _scale;
    private float _flipper;
    private Color _color;

    public static WobblyLineEffect Create(Vector2 position, Color color)
    {
        var effect = new WobblyLineEffect();
        effect.Position = position;
        effect._color = color;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/wobblyLine");

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

        _rotation = (float)GD.RandRange(0, 360);
        _scale = (float)GD.RandRange(0.2f, 0.4f);
        _speedStart = (float)GD.RandRange(300f, 1000f);
        _speedTarget = 900f;
        _speed = _speedStart;
        _flipper = GD.Randf() > 0.5f ? 90f : 270f;

        _color.G -= (float)GD.RandRange(0, 0.1f);
        _color.B -= (float)GD.RandRange(0, 0.2f);
        _color.A = 0f;

        _sprite.ZIndex = GD.Randf() > 0.5f ? -1 : 1;
        _sprite.Material = CreateAdditiveMaterial();

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

        float radians = Mathf.DegToRad(_rotation);
        float dx = Mathf.Cos(radians) * _speed * delta;
        float dy = Mathf.Sin(radians) * _speed * delta;
        Position += new Vector2(dx, dy);

        float t = 1f - Duration / StartingDuration;
        _speed = Lerp(_speedStart, _speedTarget, Smootherstep(t));

        _scale += delta;

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
        float jitterRotation = (float)GD.RandRange(-5f, 5f);
        _sprite.RotationDegrees = _rotation + _flipper + jitterRotation;

        float jitterScale = (float)GD.RandRange(-0.08f, 0.08f);
        _sprite.Scale = new Vector2(_scale + jitterScale, _scale + jitterScale);

        _sprite.Modulate = _color;
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}