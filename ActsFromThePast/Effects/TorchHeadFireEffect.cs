using Godot;

namespace ActsFromThePast;

public partial class TorchHeadFireEffect : NSts1Effect
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
    
    private static Texture2D[] _cachedTextures;

    private static Texture2D GetRandomTexture()
    {
        _cachedTextures ??= FireTextures.Select(GD.Load<Texture2D>).ToArray();
        return _cachedTextures[Random.Shared.Next(_cachedTextures.Length)];
    }

    public static TorchHeadFireEffect Create(Vector2 position)
    {
        var effect = new TorchHeadFireEffect();
        effect._x = position.X + (float)GD.RandRange(-5.0, 5.0);
        effect._y = position.Y + (float)GD.RandRange(-5.0, 5.0);
        effect._vX = (float)GD.RandRange(-30.0, 30.0);
        effect._vY = (float)GD.RandRange(-100.0, -20.0);
        effect._flippedX = GD.Randf() < 0.5f;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 0.7f;
        StartingDuration = 0.7f;

        var texture = GetRandomTexture();
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

        _scale = (float)GD.RandRange(0.8, 0.9);

        // Chartreuse color
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

        _color.A = Duration / 2f;

        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;

        _sprite.Scale = new Vector2(_scale * 1.2f, _scale * 1.2f);
        _sprite.Modulate = _color;
    }
}