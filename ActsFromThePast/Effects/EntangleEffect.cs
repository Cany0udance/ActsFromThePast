using Godot;

namespace ActsFromThePast;

public partial class EntangleEffect : NSts1Effect
{
    private const string TexturePath = "res://ActsFromThePast/vfx/web.png";
    private const float EffectDuration = 1.0f;
    private Sprite2D _sprite;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private Color _color;

    public static EntangleEffect Create(Vector2 targetPos, Vector2 startPos)
    {
        var effect = new EntangleEffect();
        effect._targetPos = targetPos - new Vector2(32f, 32f);
        effect._startPos = startPos - new Vector2(32f, 32f);
        effect.Position = startPos;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;

        var texture = GD.Load<Texture2D>(TexturePath);
        if (texture == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = texture;
        _sprite.Centered = true;
        _sprite.Material = CreateAdditiveMaterial();
        AddChild(_sprite);

        _sprite.Scale = new Vector2(0.01f, 0.01f);
        _color = new Color(1f, 1f, 1f, 0f);
    }

    protected override void Update(float delta)
    {
        if (_sprite == null)
        {
            IsDone = true;
            return;
        }

        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        float x = Lerp(_startPos.X, _targetPos.X, Pow5In(Duration));
        float y = Lerp(_startPos.Y, _targetPos.Y, Pow5In(Duration));
        Position = new Vector2(x, y);

        if (Duration > StartingDuration / 2f)
        {
            float t = Duration - StartingDuration / 2f;
            _color.A = Lerp(1f, 0.01f, Fade(t));
        }
        else
        {
            float t = Duration / (StartingDuration / 2f);
            _color.A = Lerp(0.01f, 1f, Fade(t));
        }

        float scale = Lerp(5f, 1f, BounceIn(Duration));
        _sprite.Scale = new Vector2(scale, scale);
        _sprite.Modulate = _color;
    }

    private static float Pow5In(float t) => t * t * t * t * t;

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