using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class BiteEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 1.0f;
    private Sprite2D _topSprite;
    private Sprite2D _botSprite;
    private float _topY;
    private float _topStartY;
    private float _topTargetY;
    private float _botY;
    private float _botStartY;
    private float _botTargetY;
    private Color _color;
    private bool _playedSfx;
    
    public static BiteEffect Create(Vector2 position, Color? color = null)
    {
        var effect = new BiteEffect();
        effect.Position = position;
        effect._color = color ?? new Color(0.7f, 0.9f, 1.0f, 0f);
        effect.Setup();
        return effect;
    }
    
    public static BiteEffect CreateChartreuse(Vector2 position)
    {
        return Create(position, new Color(0.5f, 1.0f, 0f, 0f));
    }
    
    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _playedSfx = false;
        
        var topRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/biteTop");
        var botRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/biteBot");
        if (topRegion == null || botRegion == null)
        {
            IsDone = true;
            return;
        }
        
        _topSprite = new Sprite2D();
        _topSprite.Texture = topRegion.Value.Texture;
        _topSprite.RegionEnabled = true;
        _topSprite.RegionRect = topRegion.Value.Region;
        _topSprite.Centered = true;
        AddChild(_topSprite);
        
        _botSprite = new Sprite2D();
        _botSprite.Texture = botRegion.Value.Texture;
        _botSprite.RegionEnabled = true;
        _botSprite.RegionRect = botRegion.Value.Region;
        _botSprite.Centered = true;
        AddChild(_botSprite);
        
        _topStartY = -150f;
        _topTargetY = 0f;
        _topY = _topStartY;
        
        _botStartY = 100f;
        _botTargetY = -10f;
        _botY = _botStartY;
        
        _topSprite.Material = CreateAdditiveMaterial();
        _botSprite.Material = CreateAdditiveMaterial();
        
        UpdateSprites();
    }
    
    protected override void Update(float delta)
    {
        Duration -= delta;
        
        if (Duration < StartingDuration - 0.3f && !_playedSfx)
        {
            _playedSfx = true;
            ModAudio.Play("general", "bite", 0f, 0.05f);
        }
        
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
        
        float halfDuration = StartingDuration / 2f;
        if (Duration > halfDuration)
        {
            float t = (StartingDuration - Duration) / halfDuration;
            _color.A = Lerp(0f, 1f, EaseOut(t));
            _topY = Lerp(_topStartY, _topTargetY, BounceIn(t));
            _botY = Lerp(_botStartY, _botTargetY, BounceIn(t));
        }
        else
        {
            float t = Duration / halfDuration;
            _color.A = Lerp(0f, 1f, EaseOut(t));
            _topY = Lerp(_topStartY, _topTargetY, EaseOut(t));
            _botY = Lerp(_botStartY, _botTargetY, EaseOut(t));
        }
        
        UpdateSprites();
    }
    
    private void UpdateSprites()
    {
        _topSprite.Position = new Vector2(0, _topY);
        _botSprite.Position = new Vector2(0, _botY);
        _topSprite.Modulate = _color;
        _botSprite.Modulate = _color;
        
        float jitter = (float)GD.RandRange(-0.05, 0.05);
        _topSprite.Scale = new Vector2(1f + jitter, 1f + jitter);
        _botSprite.Scale = new Vector2(1f + jitter, 1f + jitter);
    }
    
    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}