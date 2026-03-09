using Godot;

namespace ActsFromThePast;

public partial class StaffFireEffect : NSts1Effect
{
    private static readonly string[] FireTextures =
    {
        "res://ActsFromThePast/vfx/fire1.png",
        "res://ActsFromThePast/vfx/fire2.png"
    };

    private Sprite2D _sprite;
    private float _x;
    private float _y;
    private float _vX;
    private float _vY;
    private float _scale;
    private Color _color;
    private bool _flippedX;

    public static StaffFireEffect Create(Vector2 position)
    {
        var effect = new StaffFireEffect();
        effect._x = position.X;
        effect._y = position.Y;
        effect._vX = (float)GD.RandRange(-50.0, 50.0);
        effect._vY = (float)GD.RandRange(-140.0, -20.0);
        effect._flippedX = GD.Randf() < 0.5f;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 0.7f;
        StartingDuration = 0.7f;

        var texturePath = FireTextures[Random.Shared.Next(FireTextures.Length)];
        var texture = GD.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = texture;
        _sprite.Centered = true;
        _sprite.FlipH = _flippedX;

        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        _sprite.Material = material;

        AddChild(_sprite);

        _scale = (float)GD.RandRange(1.1, 1.2);
        _color = new Color(0.5f, 1.0f, 0.0f, 0.0f);

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

        _color.A = Duration * 0.75f;

        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;

        float scaleJitter = (float)GD.RandRange(1.05, 1.1);
        _sprite.Scale = new Vector2(_scale * scaleJitter, _scale);
        _sprite.Modulate = _color;
    }
}