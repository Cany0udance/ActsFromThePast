using Godot;

namespace ActsFromThePast;

public partial class AwakenedEyeParticle : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

    private Sprite2D _sprite;
    private Sprite2D _sprite2;
    private Color _color;
    private float _baseScale;

    public static AwakenedEyeParticle Create(Vector2 position)
    {
        var effect = new AwakenedEyeParticle();
        effect.Position = position;
        effect._baseScale = (float)GD.RandRange(0.5f, 1.0f);
        effect._color = new Color(
            (float)GD.RandRange(0.2f, 0.4f),
            (float)GD.RandRange(0.8f, 1.0f),
            (float)GD.RandRange(0.8f, 1.0f),
            0.01f
        );
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = (float)GD.RandRange(0.5f, 1.0f);
        StartingDuration = Duration;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "shine2");
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        // Horizontal stretch sprite
        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        _sprite.Material = CreateAdditiveMaterial();
        AddChild(_sprite);

        // Vertical stretch sprite
        _sprite2 = new Sprite2D();
        _sprite2.Texture = textureRegion.Value.Texture;
        _sprite2.RegionEnabled = true;
        _sprite2.RegionRect = textureRegion.Value.Region;
        _sprite2.Centered = true;
        _sprite2.Material = CreateAdditiveMaterial();
        AddChild(_sprite2);
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        float t = Duration / StartingDuration;
        _color.A = Lerp(0f, 0.5f, t);

        float scaleX1 = _baseScale * (float)GD.RandRange(6.0f, 12.0f);
        float scaleY1 = _baseScale * (float)GD.RandRange(0.7f, 0.8f);
        float rot1 = (float)GD.RandRange(-1.0f, 1.0f);
        _sprite.Scale = new Vector2(scaleX1, scaleY1);
        _sprite.RotationDegrees = rot1;
        _sprite.Modulate = _color;

        float scaleX2 = _baseScale * (float)GD.RandRange(0.2f, 0.5f);
        float scaleY2 = _baseScale * (float)GD.RandRange(2.0f, 3.0f);
        float rot2 = (float)GD.RandRange(-1.0f, 1.0f);
        _sprite2.Scale = new Vector2(scaleX2, scaleY2);
        _sprite2.RotationDegrees = rot2;
        _sprite2.Modulate = _color;
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}