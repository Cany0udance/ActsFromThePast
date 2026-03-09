using Godot;

namespace ActsFromThePast;

public partial class TimeWarpTurnEndEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/literally_just_here_for_time_warp/powers.atlas";
    private const string RegionName = "128/time";
    private Sprite2D _sprite;
    private float _x;
    private float _y;
    private Color _color;
    private float _scale;

    public static TimeWarpTurnEndEffect Create()
    {
        var effect = new TimeWarpTurnEndEffect();
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 2.0f;
        StartingDuration = 2.0f;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, RegionName);
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

        AddChild(_sprite);

        var viewport = GetViewport();
        var viewportSize = viewport?.GetVisibleRect().Size ?? new Vector2(1920, 1080);

        _scale = 3.0f;
        _color = new Color(1f, 1f, 1f, 1f);

        _x = viewportSize.X * 0.5f;

        _y = viewportSize.Y + _sprite.RegionRect.Size.Y / 2f;

        Position = new Vector2(_x, _y);
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

        if (Duration < 1.0f)
        {
            float t = Duration;
            float fade = t * t * t * (t * (t * 6f - 15f) + 10f);
            _color.A = fade;
        }
        else
        {
            float t = Mathf.Clamp(Duration - 1.0f, 0f, 1f);
            float swingIn = t * t * ((2.70158f + 1f) * t - 2.70158f);
            var viewport = GetViewport();
            var viewportSize = viewport?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            var regionHeight = _sprite.RegionRect.Size.Y;

            _y = Mathf.Lerp(viewportSize.Y + regionHeight / 2f, viewportSize.Y * 0.5f, 1.0f - swingIn);
        }

        _sprite.Rotation = Duration * Mathf.Pi * 2f;

        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
    }
}