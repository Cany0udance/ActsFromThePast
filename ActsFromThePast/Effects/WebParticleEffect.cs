using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class WebParticleEffect : NSts1Effect
{
    private const string TexturePath = "res://ActsFromThePast/vfx/web.png";
    private const float EffectDuration = 1.0f;
    
    private Sprite2D _sprite;
    private float _scale;
    private float _alpha;

    public static WebParticleEffect Create(Vector2 position)
    {
        var effect = new WebParticleEffect();
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _scale = 0.01f;
        _alpha = 0f;
        
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
        
        float halfDuration = StartingDuration / 2f;
        if (Duration > halfDuration)
        {
            float t = (StartingDuration - Duration) / halfDuration;
            _alpha = Lerp(0.01f, 1f, EaseOut(t));
        }
        else
        {
            float t = Duration / halfDuration;
            _alpha = Lerp(0.01f, 1f, EaseOut(t));
        }
        
        // StS1: elasticIn.apply(2.5, 0.01, duration/startingDuration)
        // Grows from 0.01 (at start when t=1) to 2.5 (at end when t=0)
        float t2 = Duration / StartingDuration;
        float elasticT = ElasticIn(t2);
        _scale = 2.5f + (0.01f - 2.5f) * elasticT;
        
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = new Color(1f, 1f, 1f, _alpha);
    }

    private static float ElasticIn(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        
        float p = 0.3f;
        float s = p / 4f;
        float postFix = Mathf.Pow(2f, 10f * (t - 1f));
        return -(postFix * Mathf.Sin((t - 1f - s) * (2f * Mathf.Pi) / p));
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}