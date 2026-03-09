using Godot;

namespace ActsFromThePast;
public partial class AdditiveSlashEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

    private Sprite2D _sprite;
    private Sprite2D _sprite2;
    private float _targetScale;
    private float _scale;
    private float _rotation;
    private Color _color;

    public static AdditiveSlashEffect Create(Vector2 position, Color color)
    {
        var effect = new AdditiveSlashEffect();
        effect.Position = position;
        effect._color = color;
        effect._targetScale = (float)GD.RandRange(3.0, 5.0);
        effect._rotation = (float)GD.RandRange(0.0, 360.0);
        effect._scale = 0.01f;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 0.4f;
        StartingDuration = 0.4f;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "ui/impactLineThick");
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;

        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        _sprite.Material = material;
        AddChild(_sprite);

        _sprite2 = new Sprite2D();
        _sprite2.Texture = textureRegion.Value.Texture;
        _sprite2.RegionEnabled = true;
        _sprite2.RegionRect = textureRegion.Value.Region;
        _sprite2.Centered = true;
        _sprite2.Material = material;
        AddChild(_sprite2);

        UpdateVisuals();
    }

    protected override void Update(float delta)
    {
        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        if (Duration > 0.2f)
        {
            float t = (Duration - 0.2f) * 5f;
            _color.A = Lerp(0f, 0.8f, Smootherstep(t));
            _scale = Lerp(0.01f, _targetScale, Smootherstep(t));
        }
        else
        {
            float t = Duration * 5f;
            _color.A = Lerp(0f, 0.8f, Smootherstep(t));
            _scale = Lerp(0.01f, _targetScale, Smootherstep(t));
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_sprite == null) return;

        _sprite.Scale = new Vector2(_scale / 3f, _scale);
        _sprite.RotationDegrees = _rotation;
        _sprite.Modulate = _color;

        _sprite2.Scale = new Vector2(_scale / 6f, _scale * 0.5f);
        _sprite2.RotationDegrees = _rotation + 90f;
        _sprite2.Modulate = _color;
    }
}