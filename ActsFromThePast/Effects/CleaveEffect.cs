using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class CleaveEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float FadeInTime = 0.05f;
    private const float FadeOutTime = 0.4f;
    private const float ScreenWidth = 1920f;
    private const float FloorY = 800f; // Adjust based on your floor position

    private Sprite2D _sprite;
    private Sprite2D _additiveSprite;
    
    private float _vX;
    private float _fadeInTimer;
    private float _fadeOutTimer;
    private float _stallTimer;
    private float _scale;
    private float _rotation;
    private float _alpha;

    public static CleaveEffect Create(Vector2 position)
    {
        var effect = new CleaveEffect();
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    private void SetupPosition(bool usedByMonster)
    {
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/cleave");
        if (textureRegion == null)
            return;

        float imgWidth = textureRegion.Value.Region.Size.X;
        float imgHeight = textureRegion.Value.Region.Size.Y;

        float x;
        if (usedByMonster)
        {
            x = ScreenWidth * 0.3f - imgWidth / 2f;
        }
        else
        {
            x = ScreenWidth * 0.7f - imgWidth / 2f;
        }
        
        float y = FloorY - 100f - imgHeight / 2f; // Y inverted, 100 above floor

        Position = new Vector2(x, y);
    }

    protected override void Initialize()
    {
        _fadeInTimer = FadeInTime;
        _fadeOutTimer = FadeOutTime;
        _stallTimer = (float)GD.RandRange(0f, 0.2f);
        _scale = 1.2f;
        _rotation = (float)GD.RandRange(-5f, 1f);
        _vX = 100f;
        _alpha = 0f;

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/cleave");
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        // Main sprite
        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        AddChild(_sprite);

        // Additive overlay sprite
        _additiveSprite = new Sprite2D();
        _additiveSprite.Texture = textureRegion.Value.Texture;
        _additiveSprite.RegionEnabled = true;
        _additiveSprite.RegionRect = textureRegion.Value.Region;
        _additiveSprite.Centered = true;
        _additiveSprite.Material = CreateAdditiveMaterial();
        AddChild(_additiveSprite);

        UpdateSprites();
    }

    protected override void Update(float delta)
    {
        if (_stallTimer > 0f)
        {
            _stallTimer -= delta;
            return;
        }

        Position += new Vector2(_vX * delta, 0);
        _rotation += (float)GD.RandRange(-0.5f, 0.5f);
        _scale += 0.005f;

        if (_fadeInTimer > 0f)
        {
            _fadeInTimer -= delta;
            if (_fadeInTimer < 0f)
                _fadeInTimer = 0f;
            
            float t = _fadeInTimer / FadeInTime;
            _alpha = Fade(1f - t); // Fade in from 0 to 1
        }
        else if (_fadeOutTimer > 0f)
        {
            _fadeOutTimer -= delta;
            if (_fadeOutTimer < 0f)
                _fadeOutTimer = 0f;
            
            float t = _fadeOutTimer / FadeOutTime;
            _alpha = Pow2(t); // Fade out from 1 to 0
        }
        else
        {
            IsDone = true;
            return;
        }

        UpdateSprites();
    }

    private void UpdateSprites()
    {
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.RotationDegrees = _rotation;
        _sprite.Modulate = new Color(1f, 1f, 1f, _alpha);

        _additiveSprite.Scale = new Vector2(_scale, _scale);
        _additiveSprite.RotationDegrees = _rotation;
        _additiveSprite.Modulate = new Color(1f, 1f, 1f, _alpha);
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float Pow2(float t)
    {
        return t * t;
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}