using Godot;

namespace ActsFromThePast;

public partial class SmallLaserEffect : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    private const float EffectDuration = 0.5f;
    
    private Vector2 _sourcePos;
    private Vector2 _destPos;
    private float _distance;
    private float _rotation;
    
    private Sprite2D _primaryBeam;
    private Sprite2D _secondaryBeam;
    
    private Color _primaryColor;
    private Color _secondaryColor;
    
    public static SmallLaserEffect Create(Vector2 sourcePos, Vector2 destPos)
    {
        var effect = new SmallLaserEffect();
        effect._sourcePos = sourcePos;
        effect._destPos = destPos;
        effect._distance = sourcePos.DistanceTo(destPos);
        
        // Calculate rotation: atan2 then convert to degrees
        float dx = destPos.X - sourcePos.X;
        float dy = destPos.Y - sourcePos.Y;
        effect._rotation = -Mathf.Atan2(dx, dy) * (180f / Mathf.Pi) + 90f;
        
        effect.Position = sourcePos;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
    
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/laserThin");
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }
    
        float imgHeight = textureRegion.Value.Region.Size.Y;
    
        _primaryBeam = CreateBeamSprite(textureRegion.Value, imgHeight, 50f, 10f);
        _secondaryBeam = CreateBeamSprite(textureRegion.Value, imgHeight, 70f, 0f);
    
        AddChild(_primaryBeam);
        AddChild(_secondaryBeam);
    
        _primaryColor = new Color(0f, 1f, 1f, 0f);
        _secondaryColor = new Color(0.3f, 0.3f, 1f, 0f);
    }

    private Sprite2D CreateBeamSprite(LibGdxAtlas.TextureRegion region, float imgHeight, float beamHeight, float verticalOffset)
    {
        var sprite = new Sprite2D();
        sprite.Texture = region.Texture;
        sprite.RegionEnabled = true;
        sprite.RegionRect = region.Region;
        sprite.Centered = false;
        sprite.Offset = new Vector2(0f, -imgHeight / 2f + verticalOffset);
        sprite.Scale = new Vector2(_distance / region.Region.Size.X, beamHeight / imgHeight);
        sprite.RotationDegrees = _rotation;
        sprite.Material = CreateAdditiveMaterial();
        return sprite;
    }
    
    protected override void Update(float delta)
    {
        Duration -= delta;
    
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }
    
        float alpha;
        if (Duration > StartingDuration / 2f)
        {
            float t = (Duration - 0.25f) * 4f;
            alpha = Lerp(1f, 0f, Pow2In(t));
        }
        else
        {
            float t = Duration * 4f;
            alpha = Lerp(0f, 1f, BounceIn(t));
        }
    
        _primaryColor.A = alpha;
        _secondaryColor.A = alpha;
    
        float primaryJitter = (float)GD.RandRange(-0.01f, 0.01f);
        float secondaryJitter = (float)GD.RandRange(-0.02f, 0.02f);
    
        var baseScaleX = _distance / _primaryBeam.RegionRect.Size.X;
        _primaryBeam.Scale = new Vector2(baseScaleX + primaryJitter, _primaryBeam.Scale.Y);
    
        float secondaryHeight = (float)GD.RandRange(50f, 90f);
        float imgHeight = _secondaryBeam.RegionRect.Size.Y;
        _secondaryBeam.Scale = new Vector2(baseScaleX + secondaryJitter, secondaryHeight / imgHeight);
    
        _primaryBeam.Modulate = _primaryColor;
        _secondaryBeam.Modulate = _secondaryColor;
    }
    
    private static float Pow2In(float t)
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