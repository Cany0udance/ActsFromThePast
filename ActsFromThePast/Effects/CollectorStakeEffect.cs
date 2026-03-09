using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public partial class CollectorStakeEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

    private Sprite2D _sprite;
    private Sprite2D _sprite2;
    private float _x;
    private float _y;
    private float _sX;
    private float _sY;
    private float _tX;
    private float _tY;
    private float _targetAngle;
    private float _startingAngle;
    private float _targetScale;
    private float _scale;
    private float _rotation;
    private Color _color;
    private bool _shownSlash;

    public static CollectorStakeEffect Create(Vector2 target)
    {
        var effect = new CollectorStakeEffect();

        float randomAngle = Mathf.DegToRad((float)GD.RandRange(-50.0, 230.0));
        float distX = Mathf.Cos(randomAngle) * (float)GD.RandRange(200.0, 600.0);
        float distY = Mathf.Sin(randomAngle) * (float)GD.RandRange(200.0, 500.0);
        effect._x = distX + target.X;
        effect._y = distY + target.Y;
        effect._tX = target.X;
        effect._tY = target.Y;
        effect._sX = effect._x;
        effect._sY = effect._y;

        effect._targetAngle = Mathf.RadToDeg(Mathf.Atan2(target.Y - effect._y, target.X - effect._x)) + 270f;
        effect._startingAngle = (float)GD.RandRange(0.0, 360.0);
        effect._rotation = effect._startingAngle;
        effect._targetScale = (float)GD.RandRange(0.4, 1.1);
        effect._scale = 0.01f;
        effect._shownSlash = false;

        effect._color = new Color(
            (float)GD.RandRange(0.5, 1.0),
            (float)GD.RandRange(0.0, 0.4),
            (float)GD.RandRange(0.5, 1.0),
            0f
        );

        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 1.0f;
        StartingDuration = 1.0f;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/stake");
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
            NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
            ModAudio.Play("general", "attack_fast");
            return;
        }

        _rotation = Lerp(_targetAngle, _startingAngle, ElasticIn(Duration));

        if (Duration > 0.5f)
        {
            float t = (Duration - 0.5f) * 2f;
            _scale = Lerp(_targetScale, _targetScale * 10f, ElasticIn(t));
            _color.A = Lerp(0.6f, 0f, Smootherstep(t));
        }
        else
        {
            float t = Duration * 2f;
            _x = Lerp(_tX, _sX, Exp10Out(t));
            _y = Lerp(_tY, _sY, Exp10Out(t));
        }

        if (Duration < 0.05f && !_shownSlash)
        {
            var slash = AdditiveSlashEffect.Create(new Vector2(_tX, _tY), _color);
            Node vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            vfxContainer?.AddChildSafely(slash);
            _shownSlash = true;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_sprite == null) return;

        Position = new Vector2(_x, _y);

        float jitter1X = (float)GD.RandRange(1.0, 1.2);
        float jitter1Y = (float)GD.RandRange(1.0, 1.2);
        _sprite.Scale = new Vector2(_scale * jitter1X, _scale * jitter1Y);
        _sprite.RotationDegrees = _rotation;
        _sprite.Modulate = _color;

        float jitter2X = (float)GD.RandRange(0.9, 1.1);
        float jitter2Y = (float)GD.RandRange(0.9, 1.1);
        _sprite2.Scale = new Vector2(_scale * jitter2X, _scale * jitter2Y);
        _sprite2.RotationDegrees = _rotation;
        _sprite2.Modulate = _color;
    }

    private static float ElasticIn(float a)
    {
        if (a <= 0f) return 0f;
        if (a >= 1f) return 1f;
        float p = 0.3f;
        float s = p / 4f;
        return -(Mathf.Pow(2f, 10f * (a - 1f)) * Mathf.Sin((a - 1f - s) * Mathf.Tau / p));
    }

    private static float Exp10Out(float a)
    {
        return Mathf.Clamp(1f - Mathf.Pow(2f, -10f * a), 0f, 1f);
    }
}