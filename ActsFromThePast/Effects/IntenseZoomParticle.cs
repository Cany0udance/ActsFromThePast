using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class IntenseZoomParticle : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 1.5f;

    private Sprite2D _sprite;
    private Vector2 _basePosition;
    private bool _isBlack;
    private float _flickerTimer;
    
    private float _offsetX;
    private float _lengthX;
    private float _lengthY;
    private float _alpha;

    public static IntenseZoomParticle Create(Vector2 position, bool isBlack)
    {
        var effect = new IntenseZoomParticle();
        effect._basePosition = position;
        effect._isBlack = isBlack;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _flickerTimer = 0f;

        string coneName = (GD.RandRange(0, 2)) switch
        {
            0 => "cone8",
            1 => "cone5",
            _ => "cone6"
        };

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, coneName);
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = false;
        _sprite.Offset = new Vector2(0, -textureRegion.Value.Region.Size.Y / 2f);
        
        if (!_isBlack)
        {
            _sprite.Material = CreateAdditiveMaterial();
        }
        
        AddChild(_sprite);
        
        Position = _basePosition;
        Randomize();
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        _flickerTimer -= delta;

        if (_flickerTimer < 0f)
        {
            Randomize();
            _flickerTimer = (float)GD.RandRange(0f, 0.05f);
        }

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        UpdateSprite();
    }

    private void Randomize()
    {
        RotationDegrees = (float)GD.RandRange(0f, 360f);
        
        float durationFactor = 2f - Duration;
        _offsetX = (float)GD.RandRange(200f, 600f) * durationFactor;
        _lengthX = (float)GD.RandRange(1f, 1.3f);
        _lengthY = (float)GD.RandRange(0.9f, 1.2f);

        float pow2Out = Pow2Out(Duration / EffectDuration);
        if (_isBlack)
        {
            _alpha = (float)GD.RandRange(0.5f, 1f) * pow2Out;
        }
        else
        {
            _alpha = (float)GD.RandRange(0.2f, 0.7f) * pow2Out;
        }
    }

    private void UpdateSprite()
    {
        _sprite.Scale = new Vector2(_lengthX, _lengthY);
        _sprite.Position = new Vector2(_offsetX, 0);

        var color = _isBlack ? Colors.Black : new Color(0.937f, 0.808f, 0.373f, 1f);
        _sprite.Modulate = new Color(color.R, color.G, color.B, _alpha);
    }

    private static float Pow2Out(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}