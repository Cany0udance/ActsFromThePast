using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheBeyond;

public partial class TheBeyondBackground : NCombatBackground
{
    private const string AtlasPath = "res://ActsFromThePast/backgrounds/beyond/scene.atlas";

    // Background layers
    private TextureRect _bg1, _bg1Glow;
    private TextureRect _bg2, _bg2Glow;
    private TextureRect _floor;
    private TextureRect _ceiling;
    private TextureRect _fg;

    // Midground
    private TextureRect _mg1, _mg1Glow;
    private TextureRect _mg2, _mg2Glow;
    private TextureRect _mg3, _mg3Glow;
    private TextureRect _mg4, _mg4Glow;

    // Columns
    private TextureRect _c1, _c1Glow;
    private TextureRect _c2, _c2Glow;
    private TextureRect _c3, _c3Glow;
    private TextureRect _c4, _c4Glow;

    // Floaters
    private TextureRect _f1, _f2, _f3, _f4, _f5;
    // Store original positions for animation offsets
    private Vector2 _f1Base, _f2Base, _f3Base, _f4Base, _f5Base;

    // Ice
    private TextureRect _i1, _i2, _i3, _i4, _i5;

    // Stalactites
    private TextureRect _s1, _s1Glow;
    private TextureRect _s2, _s2Glow;
    private TextureRect _s3, _s3Glow;
    private TextureRect _s4, _s4Glow;
    private TextureRect _s5, _s5Glow;

    // Render flags
    private bool _renderAltBg;
    private bool _renderM1, _renderM2, _renderM3, _renderM4;
    private bool _renderF1, _renderF2, _renderF3, _renderF4, _renderF5;
    private bool _renderIce;
    private bool _renderI1, _renderI2, _renderI3, _renderI4, _renderI5;
    private bool _renderStalactites;
    private bool _renderS1, _renderS2, _renderS3, _renderS4, _renderS5;
    private ColumnConfig _columnConfig = ColumnConfig.Open;

    // Colors
    private Color _overlayColor;
    private float _overlayGlowAlpha; // the low alpha used for the additive pass

    private bool _initialized = false;

    private enum ColumnConfig
    {
        Open,
        SmallOnly,
        SmallPlusLeft,
        SmallPlusRight
    }

    public override void _Ready()
    {
        base._Ready();
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = -100;

        // Z-index plan (back to front):
        // -60s: bg, floor, ceiling
        // -50s: bg glow pass, mg, mg glow pass
        // -40s: columns, column glow pass, stalactites, stalactite glow pass
        // -30s: ice
        // -20s: floaters
        // -10s: fg

        _floor = CreateTextureRect("mod/floor", -65);
        _ceiling = CreateTextureRect("mod/ceiling", -64);
        _bg1 = CreateTextureRect("mod/bg1", -63);
        _bg2 = CreateTextureRect("mod/bg2", -62);

        // Midground (rendered before columns in original)
        _mg2 = CreateTextureRect("mod/mod2", -59);
        _mg1 = CreateTextureRect("mod/mod1", -58);
        _mg3 = CreateTextureRect("mod/mod3", -57);
        _mg4 = CreateTextureRect("mod/mod4", -56);

        // Columns
        _c1 = CreateTextureRect("mod/c1", -50);
        _c2 = CreateTextureRect("mod/c2", -49);
        _c3 = CreateTextureRect("mod/c3", -48);
        _c4 = CreateTextureRect("mod/c4", -47);

        // Stalactites
        _s1 = CreateTextureRect("mod/s1", -45);
        _s2 = CreateTextureRect("mod/s2", -44);
        _s3 = CreateTextureRect("mod/s3", -43);
        _s4 = CreateTextureRect("mod/s4", -42);
        _s5 = CreateTextureRect("mod/s5", -41);

        // Additive glow duplicates (same textures, additive blend)
        _bg1Glow = CreateTextureRect("mod/bg1", -39);
        _bg2Glow = CreateTextureRect("mod/bg2", -38);
        _mg2Glow = CreateTextureRect("mod/mod2", -37);
        _mg1Glow = CreateTextureRect("mod/mod1", -36);
        _mg3Glow = CreateTextureRect("mod/mod3", -35);
        _mg4Glow = CreateTextureRect("mod/mod4", -34);
        _c1Glow = CreateTextureRect("mod/c1", -33);
        _c2Glow = CreateTextureRect("mod/c2", -32);
        _c3Glow = CreateTextureRect("mod/c3", -31);
        _c4Glow = CreateTextureRect("mod/c4", -30);
        _s1Glow = CreateTextureRect("mod/s1", -29);
        _s2Glow = CreateTextureRect("mod/s2", -28);
        _s3Glow = CreateTextureRect("mod/s3", -27);
        _s4Glow = CreateTextureRect("mod/s4", -26);
        _s5Glow = CreateTextureRect("mod/s5", -25);

        // Ice (rendered after glow pass in original, with full-alpha overlay)
        _i1 = CreateTextureRect("mod/i1", -23);
        _i2 = CreateTextureRect("mod/i2", -22);
        _i3 = CreateTextureRect("mod/i3", -21);
        _i4 = CreateTextureRect("mod/i4", -20);
        _i5 = CreateTextureRect("mod/i5", -19);

        // Floaters (rendered after ice, with tmpColor and animation)
        _f1 = CreateTextureRect("mod/f1", -15);
        _f2 = CreateTextureRect("mod/f2", -14);
        _f3 = CreateTextureRect("mod/f3", -13);
        _f4 = CreateTextureRect("mod/f4", -12);
        _f5 = CreateTextureRect("mod/f5", -11);

        // Foreground
        _fg = CreateTextureRect("mod/fg", -5);

        // Set additive blend on all glow layers
        SetAdditiveBlend(_bg1Glow);
        SetAdditiveBlend(_bg2Glow);
        SetAdditiveBlend(_mg1Glow);
        SetAdditiveBlend(_mg2Glow);
        SetAdditiveBlend(_mg3Glow);
        SetAdditiveBlend(_mg4Glow);
        SetAdditiveBlend(_c1Glow);
        SetAdditiveBlend(_c2Glow);
        SetAdditiveBlend(_c3Glow);
        SetAdditiveBlend(_c4Glow);
        SetAdditiveBlend(_s1Glow);
        SetAdditiveBlend(_s2Glow);
        SetAdditiveBlend(_s3Glow);
        SetAdditiveBlend(_s4Glow);
        SetAdditiveBlend(_s5Glow);

        // Store base positions for floater animation
        _f1Base = _f1.Position;
        _f2Base = _f2.Position;
        _f3Base = _f3.Position;
        _f4Base = _f4.Position;
        _f5Base = _f5.Position;

        RandomizeScene();
    }

    private void SetAdditiveBlend(TextureRect rect)
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        rect.Material = material;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().ProcessFrame += OnProcessFrame;
    }

    public override void _ExitTree()
    {
        GetTree().ProcessFrame -= OnProcessFrame;
        base._ExitTree();
    }

    private void OnProcessFrame()
    {
        if (!_initialized || !IsInsideTree()) return;
        UpdateFloaterAnimations();
    }

    private void UpdateFloaterAnimations()
    {
        ulong ms = Time.GetTicksMsec();
        float scale = 1.0f;

        if (_renderF1)
        {
            float offsetX = Mathf.Cos(Mathf.DegToRad((float)((ms + 180) / 180 % 360))) * 40f * scale;
            float offsetY = Mathf.Cos(Mathf.DegToRad((float)((ms + 500) / 72 % 360))) * 20f * scale;
            float rotDeg = Mathf.Cos(Mathf.DegToRad((float)((ms + 180) / 180 % 360)));
            _f1.Position = _f1Base + new Vector2(offsetX, offsetY);
            _f1.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF2)
        {
            float offsetX = Mathf.Cos(Mathf.DegToRad((float)((ms + 91723) / 72 % 360))) * 20f;
            float rotDeg = (float)(ms / 120 % 360);
            _f2.Position = _f2Base + new Vector2(offsetX, 0);
            _f2.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF3)
        {
            float offsetX = -80f * scale;
            float offsetY = Mathf.Cos(Mathf.DegToRad((float)(ms + 73))) * 10f - 90f * scale;
            float rotDeg = (float)(ms / 1000 % 360) * 2f;
            _f3.Position = _f3Base + new Vector2(offsetX, offsetY);
            _f3.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF4)
        {
            float offsetY = Mathf.Cos(Mathf.DegToRad((float)((ms + 4442) / 20 % 360))) * 30f * scale;
            float rotDeg = Mathf.Cos(Mathf.DegToRad((float)((ms + 4442) / 10 % 360))) * 20f;
            _f4.Position = _f4Base + new Vector2(0, offsetY);
            _f4.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF5)
        {
            float offsetY = Mathf.Cos(Mathf.DegToRad((float)(ms / 48 % 360))) * 20f;
            _f5.Position = _f5Base + new Vector2(0, offsetY);
            _f5.Rotation = 0;
        }
    }

    private TextureRect CreateTextureRect(string regionName, int zIndex)
    {
        var rect = new TextureRect();
        rect.MouseFilter = MouseFilterEnum.Ignore;
        rect.ZIndex = zIndex;

        var regionInfo = LibGdxAtlas.GetRegionData(AtlasPath, regionName);
        var region = LibGdxAtlas.GetRegion(AtlasPath, regionName);

        if (region != null && regionInfo != null)
        {
            var atlasTexture = new AtlasTexture();
            atlasTexture.Atlas = region.Value.Texture;
            atlasTexture.Region = region.Value.Region;

            rect.Texture = atlasTexture;
            rect.StretchMode = TextureRect.StretchModeEnum.Keep;

            float offsetX = regionInfo.Value.OffsetX - (regionInfo.Value.OrigWidth / 2f) - 23f;
            float offsetY = regionInfo.Value.OrigHeight - regionInfo.Value.OffsetY - regionInfo.Value.Height
                            - (regionInfo.Value.OrigHeight / 2f);

            rect.Position = new Vector2(offsetX, offsetY);
            rect.Size = new Vector2(regionInfo.Value.Width, regionInfo.Value.Height);
        }

        AddChild(rect);
        return rect;
    }

    public void RandomizeScene()
    {
        _overlayColor = new Color(
            (float)GD.RandRange(0.7, 0.9),
            (float)GD.RandRange(0.7, 0.9),
            (float)GD.RandRange(0.7, 1.0),
            1.0f
        );
        _overlayGlowAlpha = (float)GD.RandRange(0.0, 0.2);

        _renderAltBg = GD.Randf() < 0.2f;

        _renderM1 = false;
        _renderM2 = false;
        _renderM3 = false;
        _renderM4 = false;
        if (!_renderAltBg && GD.Randf() < 0.8f)
        {
            _renderM1 = GD.Randf() > 0.5f;
            _renderM2 = GD.Randf() > 0.5f;
            _renderM3 = GD.Randf() > 0.5f;
            if (!_renderM3)
                _renderM4 = GD.Randf() > 0.5f;
        }

        // Column config
        if (GD.Randf() < 0.6f)
            _columnConfig = ColumnConfig.Open;
        else if (GD.Randf() > 0.5f)
            _columnConfig = ColumnConfig.SmallOnly;
        else if (GD.Randf() > 0.5f)
            _columnConfig = ColumnConfig.SmallPlusLeft;
        else
            _columnConfig = ColumnConfig.SmallPlusRight;

        // Floaters (max 2, each 25% chance, 30% chance of none)
        _renderF1 = false;
        _renderF2 = false;
        _renderF3 = false;
        _renderF4 = false;
        _renderF5 = false;

        int floaterCount = 0;
        _renderF1 = GD.Randf() < 0.25f;
        if (_renderF1) floaterCount++;
        _renderF2 = GD.Randf() < 0.25f;
        if (_renderF2) floaterCount++;
        if (floaterCount < 2)
        {
            _renderF3 = GD.Randf() < 0.25f;
            if (_renderF3) floaterCount++;
        }
        if (floaterCount < 2)
        {
            _renderF4 = GD.Randf() < 0.25f;
            if (_renderF4) floaterCount++;
        }
        if (floaterCount < 2)
            _renderF5 = GD.Randf() < 0.25f;

        // 30% chance to disable all floaters
        if (GD.Randf() < 0.3f)
        {
            _renderF1 = false;
            _renderF2 = false;
            _renderF3 = false;
            _renderF4 = false;
            _renderF5 = false;
        }

        // Ice
        _renderIce = GD.Randf() > 0.5f;
        if (_renderIce)
        {
            _renderI1 = GD.Randf() > 0.5f;
            _renderI2 = GD.Randf() > 0.5f;
            _renderI3 = GD.Randf() > 0.5f;
            _renderI4 = GD.Randf() > 0.5f;
            _renderI5 = GD.Randf() > 0.5f;
        }
        else
        {
            _renderI1 = false;
            _renderI2 = false;
            _renderI3 = false;
            _renderI4 = false;
            _renderI5 = false;
        }

        // Stalactites
        _renderStalactites = GD.Randf() > 0.5f;
        if (_renderStalactites)
        {
            _renderS1 = GD.Randf() > 0.5f;
            _renderS2 = GD.Randf() > 0.5f;
            _renderS3 = GD.Randf() > 0.5f;
            _renderS4 = GD.Randf() > 0.5f;
            _renderS5 = GD.Randf() > 0.5f;
        }
        else
        {
            _renderS1 = false;
            _renderS2 = false;
            _renderS3 = false;
            _renderS4 = false;
            _renderS5 = false;
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        // Compute tmpColor: halfway between overlay and white
        var tmpColor = new Color(
            (1f + _overlayColor.R) / 2f,
            (1f + _overlayColor.G) / 2f,
            (1f + _overlayColor.B) / 2f,
            1f
        );

        // Glow color: overlay color at low alpha
        var glowColor = new Color(_overlayColor.R, _overlayColor.G, _overlayColor.B, _overlayGlowAlpha);

        // Normal pass: full-alpha overlay color
        _floor.Modulate = _overlayColor;
        _ceiling.Modulate = _overlayColor;
        _bg1.Modulate = _overlayColor;
        _bg2.Modulate = _overlayColor;
        _mg1.Modulate = _overlayColor;
        _mg2.Modulate = _overlayColor;
        _mg3.Modulate = _overlayColor;
        _mg4.Modulate = _overlayColor;
        _c1.Modulate = _overlayColor;
        _c2.Modulate = _overlayColor;
        _c3.Modulate = _overlayColor;
        _c4.Modulate = _overlayColor;
        _s1.Modulate = _overlayColor;
        _s2.Modulate = _overlayColor;
        _s3.Modulate = _overlayColor;
        _s4.Modulate = _overlayColor;
        _s5.Modulate = _overlayColor;

        // Glow pass: additive layers at low alpha
        _bg1Glow.Modulate = glowColor;
        _bg2Glow.Modulate = glowColor;
        _mg1Glow.Modulate = glowColor;
        _mg2Glow.Modulate = glowColor;
        _mg3Glow.Modulate = glowColor;
        _mg4Glow.Modulate = glowColor;
        _c1Glow.Modulate = glowColor;
        _c2Glow.Modulate = glowColor;
        _c3Glow.Modulate = glowColor;
        _c4Glow.Modulate = glowColor;
        _s1Glow.Modulate = glowColor;
        _s2Glow.Modulate = glowColor;
        _s3Glow.Modulate = glowColor;
        _s4Glow.Modulate = glowColor;
        _s5Glow.Modulate = glowColor;

        // Ice: full-alpha overlay
        _i1.Modulate = _overlayColor;
        _i2.Modulate = _overlayColor;
        _i3.Modulate = _overlayColor;
        _i4.Modulate = _overlayColor;
        _i5.Modulate = _overlayColor;

        // Floaters and fg: tmpColor
        _f1.Modulate = tmpColor;
        _f2.Modulate = tmpColor;
        _f3.Modulate = tmpColor;
        _f4.Modulate = tmpColor;
        _f5.Modulate = tmpColor;
        _fg.Modulate = tmpColor;

        // Visibility
        _bg2.Visible = _renderAltBg;
        _bg2Glow.Visible = _renderAltBg;
        _mg1.Visible = _renderM1;
        _mg1Glow.Visible = _renderM1;
        _mg2.Visible = _renderM2;
        _mg2Glow.Visible = _renderM2;
        _mg3.Visible = _renderM3;
        _mg3Glow.Visible = _renderM3;
        _mg4.Visible = _renderM4;
        _mg4Glow.Visible = _renderM4;

        // Columns
        bool showC1 = _columnConfig != ColumnConfig.Open;
        bool showC2 = _columnConfig == ColumnConfig.SmallPlusLeft;
        bool showC3 = _columnConfig == ColumnConfig.SmallPlusRight;
        bool showC4 = _columnConfig != ColumnConfig.Open;

        _c1.Visible = showC1;
        _c1Glow.Visible = showC1;
        _c2.Visible = showC2;
        _c2Glow.Visible = showC2;
        _c3.Visible = showC3;
        _c3Glow.Visible = showC3;
        _c4.Visible = showC4;
        _c4Glow.Visible = showC4;

        // Stalactites
        _s1.Visible = _renderS1;
        _s1Glow.Visible = _renderS1;
        _s2.Visible = _renderS2;
        _s2Glow.Visible = _renderS2;
        _s3.Visible = _renderS3;
        _s3Glow.Visible = _renderS3;
        _s4.Visible = _renderS4;
        _s4Glow.Visible = _renderS4;
        _s5.Visible = _renderS5;
        _s5Glow.Visible = _renderS5;

        // Ice
        _i1.Visible = _renderI1;
        _i2.Visible = _renderI2;
        _i3.Visible = _renderI3;
        _i4.Visible = _renderI4;
        _i5.Visible = _renderI5;

        // Floaters
        _f1.Visible = _renderF1;
        _f2.Visible = _renderF2;
        _f3.Visible = _renderF3;
        _f4.Visible = _renderF4;
        _f5.Visible = _renderF5;
    }

    public void OnTreeEntered()
    {
        TreeEntered -= OnTreeEntered;
        GetTree().ProcessFrame += OnProcessFrame;
        Initialize();

        Node parent = GetParent();
        while (parent != null && parent is not NCombatRoom)
            parent = parent.GetParent();

        if (parent is NCombatRoom combatRoom)
        {
            var allyContainer = combatRoom.GetNodeOrNull<Control>("%AllyContainer");
            var enemyContainer = combatRoom.GetNodeOrNull<Control>("%EnemyContainer");

            if (allyContainer != null)
                allyContainer.Position += Vector2.Down * 30f;
            if (enemyContainer != null)
                enemyContainer.Position += Vector2.Down * 30f;
        }
    }

    public void SetBossMode()
    {
        // Beyond boss fights don't have special scene elements like The City's throne,
        // but the original still silences BGM in nextRoom for boss rooms.
        // Add any boss-specific visual overrides here if needed.
    }
}