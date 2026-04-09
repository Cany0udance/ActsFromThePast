using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ActsFromThePast;

public partial class AwakenedWingParticle : NSts1Effect
{
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

    private Sprite2D _glowSprite;
    private Sprite2D _mainSprite;
    private Sprite2D _shadowSprite;
    private Color _color;
    private Color _glowColor;
    private float _tScale;
    private float _rotation;
    private Vector2 _offset;
    private NCreature _creatureNode;
    private GodotObject _bone;
    public bool RenderBehind { get; private set; }

    public static AwakenedWingParticle Create(NCreature creatureNode, GodotObject bone)
    {
        var effect = new AwakenedWingParticle();
        effect._creatureNode = creatureNode;
        effect._bone = bone;
        effect.Setup();
        return effect;
    }

    private Vector2 GetBoneWorldPos()
    {
        if (_creatureNode == null || _bone == null)
        {
            return Vector2.Zero;
        }
        var boneX = (float)_bone.Call("get_world_x");
        var boneY = (float)_bone.Call("get_world_y");
        var pos = new Vector2(
            _creatureNode.GlobalPosition.X + boneX * 1.1f,
            _creatureNode.GlobalPosition.Y + boneY * 1.1f - 20f
        );
        return pos;
    }

    protected override void Initialize()
    {
        Duration = 2.0f;
        StartingDuration = 2.0f;

        _rotation = -(float)GD.RandRange(25.0f, 85.0f);
        RenderBehind = GD.Randf() < 0.2f;

        if (RenderBehind)
        {
            _tScale = (float)GD.RandRange(0.8f, 1.2f);
        }

        _color = new Color(0.3f, 0.3f, (float)GD.RandRange(0.3f, 0.35f), (float)GD.RandRange(0.5f, 0.9f));
        _glowColor = new Color(0.4f, 1.0f, 1.0f, _color.A / 2.0f);

        float x, y;
        int roll = GD.RandRange(0, 2);
        if (roll == 0)
        {
            x = (float)GD.RandRange(-340.0f, -170.0f);
            y = (float)GD.RandRange(-20.0f, 20.0f);
            _tScale = (float)GD.RandRange(0.4f, 0.5f);
        }
        else if (roll == 1)
        {
            x = (float)GD.RandRange(-220.0f, -20.0f);
            y = (float)GD.RandRange(-40.0f, -10.0f);
            _tScale = (float)GD.RandRange(0.4f, 0.5f);
        }
        else
        {
            x = (float)GD.RandRange(-270.0f, -60.0f);
            y = (float)GD.RandRange(-30.0f, 0.0f);
            _tScale = (float)GD.RandRange(0.4f, 0.7f);
        }

        x += 155.0f;
        y += 30.0f;
        _offset = new Vector2(x - 50f, -y - 30f);

        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "combat/spike2");
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        _glowSprite = CreateSprite(textureRegion.Value, true);
        _mainSprite = CreateSprite(textureRegion.Value, false);
        _shadowSprite = CreateSprite(textureRegion.Value, false);

        if (RenderBehind)
        {
            _glowSprite.ZIndex = -1;
            _mainSprite.ZIndex = -1;
            _shadowSprite.ZIndex = -1;
        }

        AddChild(_glowSprite);
        AddChild(_mainSprite);
        AddChild(_shadowSprite);
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        GlobalPosition = GetBoneWorldPos();

        float scale;
        if (Duration > 1.0f)
        {
            float t = Duration - 1.0f;
            scale = BounceIn(_tScale, 0.01f, t);
        }
        else
        {
            scale = _tScale;
        }

        if (Duration < 0.2f)
        {
            float alpha = Lerp(0f, 0.5f, Duration * 5.0f);
            _color.A = alpha;
            _glowColor.A = alpha / 2.0f;
        }

        float derp = (float)GD.RandRange(3.0f, 5.0f);
        float rot = _rotation + derp;

        _glowSprite.Scale = new Vector2(scale * (float)GD.RandRange(1.1f, 1.25f), scale);
        _glowSprite.RotationDegrees = rot;
        _glowSprite.Modulate = _glowColor;
        _glowSprite.Position = _offset;

        _mainSprite.Scale = new Vector2(scale, scale);
        _mainSprite.RotationDegrees = rot;
        _mainSprite.Modulate = _color;
        _mainSprite.Position = _offset;

        var shadowColor = new Color(0f, 0f, 0f, _color.A / 5.0f);
        _shadowSprite.Scale = new Vector2(scale * 0.7f, scale * 0.7f);
        _shadowSprite.RotationDegrees = rot - 40.0f;
        _shadowSprite.Modulate = shadowColor;
        _shadowSprite.Position = _offset;
    }

    private static Sprite2D CreateSprite(LibGdxAtlas.TextureRegion region, bool additive)
    {
        var sprite = new Sprite2D();
        sprite.Texture = region.Texture;
        sprite.RegionEnabled = true;
        sprite.RegionRect = region.Region;
        sprite.Centered = true;
        if (additive)
        {
            sprite.Material = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Add
            };
        }
        return sprite;
    }

    private static float BounceIn(float start, float end, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        t = 1f - t;
        float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.Pi * 2.5f)) * (1f - t);
        return Mathf.Lerp(start, end, bounce);
    }
}