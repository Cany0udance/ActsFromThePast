using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.Exordium;

public partial class ExordiumBackground : NCombatBackground
{
    private const string LOG_TAG = "[ActsFromThePast]";
    private const string AtlasPath = "res://ActsFromThePast/backgrounds/exordium/scene.atlas";
    
    // Background layers
    private TextureRect _bg;
    private TextureRect _mg;
    private TextureRect _fg;
    private TextureRect _ceiling;
    
    // Wall variants
    private TextureRect _leftWall;
    private TextureRect _hollowWall;
    private TextureRect _solidWall;
    
    // Ceiling mods
    private TextureRect _ceilingMod1;
    private TextureRect _ceilingMod2;
    private TextureRect _ceilingMod3;
    private TextureRect _ceilingMod4;
    private TextureRect _ceilingMod5;
    private TextureRect _ceilingMod6;
    
    // Render flags
    private bool _renderLeftWall;
    private bool _renderHollowMid;
    private bool _renderSolidMid;
    
    // Effects
    private List<DustEffect> _dust = new();
    private List<BottomFogEffect> _fog = new();
    private const int MaxDust = 96;
    private const int MaxFog = 50;
    private List<InteractableTorchEffect> _torches = new();
    
    // Overlay
    private ColorRect _overlayRect;
    private Color _overlayColor;
    
    private bool _initialized = false;
    
    public override void _Ready()
    {
        base._Ready();
        Initialize();
    }

public void Initialize()
{
    if (_initialized) return;
    _initialized = true;
    
    try
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = -100;
        
        // Create layers in order (back to front)
        _bg = CreateTextureRect("bg", -50);
        _mg = CreateTextureRect("mod/mg", -40);
        _solidWall = CreateTextureRect("mod/midWall", -30);
        _hollowWall = CreateTextureRect("mod/mod2", -29);
        _leftWall = CreateTextureRect("mod/mod1", -28);
        _ceiling = CreateTextureRect("mod/ceiling", -20);
        _ceilingMod1 = CreateTextureRect("mod/ceilingMod1", -19);
        _ceilingMod2 = CreateTextureRect("mod/ceilingMod2", -18);
        _ceilingMod3 = CreateTextureRect("mod/ceilingMod3", -17);
        _ceilingMod4 = CreateTextureRect("mod/ceilingMod4", -16);
        _ceilingMod5 = CreateTextureRect("mod/ceilingMod5", -15);
        _ceilingMod6 = CreateTextureRect("mod/ceilingMod6", -14);
        _fg = CreateTextureRect("mod/fg", -10);

        _overlayRect = new ColorRect();
        _overlayRect.MouseFilter = MouseFilterEnum.Ignore;
        _overlayRect.ZIndex = -5;
        _overlayRect.Position = new Vector2(-983, -568);
        _overlayRect.Size = new Vector2(1920, 1136);
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        _overlayRect.Material = material;
        AddChild(_overlayRect);
        
        RandomizeScene();
    }
    catch (Exception e)
    {

    }
}

public void _DeferredInit()
{
    
    if (IsInsideTree())
    {
        GetTree().ProcessFrame += OnProcessFrame;
    }
    
    Initialize();
}

public override void _EnterTree()
{
    base._EnterTree();
    GetTree().ProcessFrame += OnProcessFrame;
}

    
    private void OnProcessFrame()
    {
        if (!_initialized || !IsInsideTree()) return;
        
        UpdateDust();
        UpdateFog();
    }
    
    public override void _ExitTree()
    {
        GetTree().ProcessFrame -= OnProcessFrame;
        base._ExitTree();
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
            float offsetY = regionInfo.Value.OrigHeight - regionInfo.Value.OffsetY - regionInfo.Value.Height -
                            (regionInfo.Value.OrigHeight / 2f);
            
            rect.Position = new Vector2(offsetX, offsetY);
            rect.Size = new Vector2(regionInfo.Value.Width, regionInfo.Value.Height);
        }
        else
        {

        }
        
        AddChild(rect);
        return rect;
    }
    
    private void RandomizeTorches()
{
    // Clear existing torches
    foreach (var torch in _torches)
    {
        if (IsInstanceValid(torch))
        {
            torch.QueueFree();
        }
    }
    _torches.Clear();
    
    // 10% chance for torch on right side
    if (GD.Randf() < 0.1f)
    {
        _torches.Add(InteractableTorchEffect.Create(1790f, 850f, InteractableTorchEffect.TorchSize.S));
    }
    
    if (_renderHollowMid && !_renderSolidMid)
    {
        int roll = (int)(GD.Randi() % 3);
        if (roll == 0)
        {
            _torches.Add(InteractableTorchEffect.Create(800f, 768f));
            _torches.Add(InteractableTorchEffect.Create(1206f, 768f));
        }
        else if (roll == 1)
        {
            _torches.Add(InteractableTorchEffect.Create(328f, 865f, InteractableTorchEffect.TorchSize.S));
        }
    }
    else if (!_renderLeftWall && !_renderHollowMid)
    {
        if (GD.Randf() < 0.75f)
        {
            _torches.Add(InteractableTorchEffect.Create(613f, 860f));
            _torches.Add(InteractableTorchEffect.Create(613f, 672f));
            if (GD.Randf() < 0.3f)
            {
                _torches.Add(InteractableTorchEffect.Create(1482f, 860f));
                _torches.Add(InteractableTorchEffect.Create(1482f, 672f));
            }
        }
    }
    else if (_renderSolidMid && _renderHollowMid)
    {
        if (!_renderLeftWall)
        {
            int roll = (int)(GD.Randi() % 4);
            if (roll == 0)
            {
                _torches.Add(InteractableTorchEffect.Create(912f, 790f));
                _torches.Add(InteractableTorchEffect.Create(912f, 526f));
                _torches.Add(InteractableTorchEffect.Create(844f, 658f, InteractableTorchEffect.TorchSize.S));
                _torches.Add(InteractableTorchEffect.Create(980f, 658f, InteractableTorchEffect.TorchSize.S));
            }
            else if (roll == 1 || roll == 2)
            {
                _torches.Add(InteractableTorchEffect.Create(1828f, 720f));
            }
        }
        else if (GD.Randf() < 0.75f)
        {
            _torches.Add(InteractableTorchEffect.Create(970f, 874f, InteractableTorchEffect.TorchSize.L));
        }
    }
    else if (_renderLeftWall && !_renderHollowMid)
    {
        if (GD.Randf() < 0.75f)
        {
            _torches.Add(InteractableTorchEffect.Create(970f, 873f, InteractableTorchEffect.TorchSize.L));
            _torches.Add(InteractableTorchEffect.Create(616f, 813f));
            _torches.Add(InteractableTorchEffect.Create(1266f, 708f));
        }
    }
    
    // Randomize green rendering for torch effects
    InteractableTorchEffect.RenderGreen = GD.Randf() > 0.5f;
    
    // Add torches to scene
    foreach (var torch in _torches)
    {
        torch.ZIndex = -15;
        AddChild(torch);
        torch.Initialize();
    }
}
    
    public void RandomizeScene()
    {
        // Randomize wall configuration (matches original logic)
        if (GD.Randf() > 0.5f)
        {
            _renderSolidMid = false;
            _renderLeftWall = false;
            _renderHollowMid = true;
        
            if (GD.Randf() > 0.5f)
            {
                _renderSolidMid = true;
                if (GD.Randf() > 0.5f)
                {
                    _renderLeftWall = true;
                }
            }
        }
        else
        {
            _renderLeftWall = false;
            _renderHollowMid = false;
            _renderSolidMid = true;
        
            if (GD.Randf() > 0.5f)
            {
                _renderLeftWall = true;
            }
        }
    
        // Randomize ceiling mods
        _ceilingMod1.Visible = GD.Randf() > 0.5f;
        _ceilingMod2.Visible = GD.Randf() > 0.5f;
        _ceilingMod3.Visible = GD.Randf() > 0.5f;
        _ceilingMod4.Visible = GD.Randf() > 0.5f;
        _ceilingMod5.Visible = GD.Randf() > 0.5f;
        _ceilingMod6.Visible = GD.Randf() > 0.5f;
    
        // Randomize overlay color (StS1 values with Godot correction factor)
        const float correctionFactor = 10f; // divide colors by this correction factor to go back to previous approach
        const float blendFactor = 0.2f;
        _overlayColor = new Color(
            (float)GD.RandRange(0.0, 0.05) * blendFactor,
            (float)GD.RandRange(0.0, 0.2) * blendFactor,
            (float)GD.RandRange(0.0, 0.2) * blendFactor,
            1.0f
        );
    
        // Randomize torches
        RandomizeTorches();
    
        UpdateVisibility();
    }
    
    private void UpdateVisibility()
    {
        _solidWall.Visible = _renderSolidMid;
        _hollowWall.Visible = _renderHollowMid;
        _leftWall.Visible = _renderLeftWall;
    
        if (_renderHollowMid && (_renderSolidMid || _renderLeftWall))
        {
            _solidWall.Modulate = Colors.Gray;
        }
        else
        {
            _solidWall.Modulate = Colors.White;
        }
    
        _overlayRect.Color = _overlayColor;
    }
    
    private void UpdateDust()
    {
        for (int i = _dust.Count - 1; i >= 0; i--)
        {
            var dust = _dust[i];
            if (dust.IsDone || !IsInstanceValid(dust))
            {
                if (IsInstanceValid(dust))
                {
                    dust.QueueFree();
                }
                _dust.RemoveAt(i);
            }
        }
        
        while (_dust.Count < MaxDust)
        {
            var dust = DustEffect.Create();
            dust.ZIndex = -11;
            AddChild(dust);
            _dust.Add(dust);
        }
    }
    
    private void UpdateFog()
    {
        for (int i = _fog.Count - 1; i >= 0; i--)
        {
            var fog = _fog[i];
            if (fog.IsDone || !IsInstanceValid(fog))
            {
                if (IsInstanceValid(fog))
                {
                    fog.QueueFree();
                }
                _fog.RemoveAt(i);
            }
        }
        
        while (_fog.Count < MaxFog)
        {
            var fog = BottomFogEffect.Create();
            fog.ZIndex = -45;
            AddChild(fog);
            _fog.Add(fog);
        }
    }
    
    public void OnTreeEntered()
    {
    
        TreeEntered -= OnTreeEntered;
        GetTree().ProcessFrame += OnProcessFrame;
        Initialize();
    
        // Find the combat room by walking up the tree
        Node parent = GetParent();
        while (parent != null && parent is not NCombatRoom)
        {
            parent = parent.GetParent();
        }
    
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
}