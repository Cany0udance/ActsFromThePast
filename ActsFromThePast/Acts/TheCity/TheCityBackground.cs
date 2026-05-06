using ActsFromThePast.Patches.Acts;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheCity;

public partial class TheCityBackground : NCombatBackground
{
    private const string AtlasPath = "res://ActsFromThePast/backgrounds/city/scene.atlas";
    
// Background layers
    private TextureRect _bg;
    private TextureRect _bgGlow;
    private TextureRect _bgGlow2;
    private TextureRect _bg2;
    private TextureRect _bg2Glow;
    private TextureRect _bgGlow2Double;  // NEW: double render for dark day
    private TextureRect _floor;
    private TextureRect _ceiling;
    private TextureRect _wall;
    private TextureRect _chains;
    private TextureRect _chainsGlow;
    private TextureRect _chainsGlow2;  // NEW: double render for chains
    private TextureRect _mg;
    private TextureRect _mgGlow;
    private TextureRect _mgGlow2;  // NEW: double render for mg glow
    private TextureRect _mgAlt;  // RENAMED: was _mg2
    private TextureRect _fg;
    private TextureRect _fgGlow;
    private TextureRect _fg2;
    private TextureRect _throne;
    private TextureRect _throneGlow;
    
    // Pillars
    private TextureRect _pillar1;
    private TextureRect _pillar2;
    private TextureRect _pillar3;
    private TextureRect _pillar4;
    private TextureRect _pillar5;
    
    // Render flags
    private bool _renderAltBg;
    private bool _renderMg;
    private bool _renderMgGlow;
    private bool _renderMgAlt;
    private bool _renderWall;
    private bool _renderChains;
    private bool _renderThrone;
    private bool _renderFg2;
    private bool _darkDay;
    private PillarConfig _pillarConfig = PillarConfig.Open;
    
    // Effects
    private List<FireFlyEffect> _fireFlies = new();
    private bool _hasFlies;
    private bool _blueFlies;
    private List<NSts1Effect> _ceilingDustEffects = new();
    private float _ceilingDustTimer = 1.0f;
    
    // Colors
    private Color _overlayColor = Colors.White;
    private ColorRect _overlayRect;
    
    private bool _initialized = false;
    
    private enum PillarConfig
    {
        Open,
        SidesOnly,
        Full,
        Left1,
        Left2
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
   
    // Create layers in order (back to front)
    _bg = CreateTextureRect("mod/bg1", -50);
    _bgGlow = CreateTextureRect("mod/bgGlowv2", -49);
    _bgGlow2 = CreateTextureRect("mod/bgGlowBlur", -48);
    _bg2 = CreateTextureRect("mod/bg2", -46);
    _bg2Glow = CreateTextureRect("mod/bg2Glow", -45);
    _bgGlow2 = CreateTextureRect("mod/bgGlowBlur", -48);
    _bgGlow2Double = CreateTextureRect("mod/bgGlowBlur", -47);  // NEW: same texture
    _floor = CreateTextureRect("mod/floor", -45);
    _ceiling = CreateTextureRect("mod/ceiling", -44);
    _wall = CreateTextureRect("mod/wall", -43);
    _chains = CreateTextureRect("mod/chains", -42);
    _chainsGlow = CreateTextureRect("mod/chainsGlow", -41);
    _chainsGlow2 = CreateTextureRect("mod/chainsGlow", -40);  // NEW: same texture, double render
    _mg = CreateTextureRect("mod/mg1", -39);
    _mgGlow = CreateTextureRect("mod/mg1Glow", -38);
    _mgGlow2 = CreateTextureRect("mod/mg1Glow", -37);  // NEW: same texture, double render
    _mgAlt = CreateTextureRect("mod/mg2", -36);  // RENAMED
    _pillar1 = CreateTextureRect("mod/p1", -35);
    _pillar2 = CreateTextureRect("mod/p2", -34);
    _pillar3 = CreateTextureRect("mod/p3", -33);
    _pillar4 = CreateTextureRect("mod/p4", -32);
    _pillar5 = CreateTextureRect("mod/p5", -31);
    _throne = CreateTextureRect("mod/throne", -30);
    _throneGlow = CreateTextureRect("mod/throneGlow", -29);
    _fg = CreateTextureRect("mod/fg", -20);
    _fgGlow = CreateTextureRect("mod/fgGlow", -19);
    _fg2 = CreateTextureRect("mod/fgHideWindow", -18);
   
    // Set up additive blend for glow layers
    SetAdditiveBlend(_bgGlow);
    SetAdditiveBlend(_bgGlow2);
    SetAdditiveBlend(_bgGlow2Double);
    SetAdditiveBlend(_bg2Glow);
    SetAdditiveBlend(_chainsGlow);
    SetAdditiveBlend(_chainsGlow2);
    SetAdditiveBlend(_mgGlow);
    SetAdditiveBlend(_mgGlow2);
    SetAdditiveBlend(_throneGlow);
    SetAdditiveBlend(_fgGlow);
   
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
    
        UpdateFireFlies();
        UpdateGlowAnimations();
        UpdateCeilingDust((float)GetProcessDeltaTime());
    }
    
    private void UpdateCeilingDust(float delta)
    {
        // Clean up finished effects
        for (int i = _ceilingDustEffects.Count - 1; i >= 0; i--)
        {
            var effect = _ceilingDustEffects[i];
            if (effect.IsDone || !IsInstanceValid(effect))
            {
                if (IsInstanceValid(effect))
                    effect.QueueFree();
                _ceilingDustEffects.RemoveAt(i);
            }
        }
    
        _ceilingDustTimer -= delta;
        if (_ceilingDustTimer < 0f)
        {
            int roll = (int)(GD.Randi() % 5); // 0-4
        
            if (roll == 0)
            {
                SpawnCeilingDust();
                PlayDustSfx(false);
            }
            else if (roll == 1)
            {
                SpawnCeilingDust();
                SpawnCeilingDust(); 
                PlayDustSfx(false);
            }
            else
            {
                SpawnCeilingDust();
                SpawnCeilingDust();
                SpawnCeilingDust();
                PlayDustSfx(true);
            }
        
            _ceilingDustTimer = (float)GD.RandRange(0.5, 60.0);
        }
    }
    
    private void PlayDustSfx(bool boom)
    {
        int roll = (int)(GD.Randi() % 3);
    
        if (boom)
        {
            string sound = roll switch
            {
                0 => "ceiling_boom_1",
                1 => "ceiling_boom_2",
                _ => "ceiling_boom_3"
            };
            ModAudio.Play("general", sound, 0f, 0.2f);
        }
        else
        {
            string sound = roll switch
            {
                0 => "ceiling_dust_1",
                1 => "ceiling_dust_2",
                _ => "ceiling_dust_3"
            };
            ModAudio.Play("general", sound, 0f, 0.2f);
        }
    }

    private void SpawnCeilingDust()
    {
        var dust = CeilingDustEffect.Create(AddCeilingDustEffect);
        dust.ZIndex = -10;
        AddChild(dust);
        _ceilingDustEffects.Add(dust);
    }

    private void AddCeilingDustEffect(NSts1Effect effect)
    {
        effect.ZIndex = -10;
        AddChild(effect);
        _ceilingDustEffects.Add(effect);
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
        
        AddChild(rect);
        return rect;
    }
    
    public void RandomizeScene()
    {
        _hasFlies = GD.Randf() > 0.5f;
        _blueFlies = GD.Randf() > 0.5f;
        
        // Overlay color tinting
        _overlayColor = new Color(
            (float)GD.RandRange(0.8, 0.9),
            (float)GD.RandRange(0.8, 0.9),
            (float)GD.RandRange(0.95, 1.0),
            1.0f
        );
        
        _darkDay = GD.Randf() < 0.33f;
        if (_darkDay)
        {
            _overlayColor = new Color(
                0.6f,
                (float)GD.RandRange(0.7, 0.8),
                (float)GD.RandRange(0.8, 0.95),
                1.0f
            );
        }
        
        _renderAltBg = GD.Randf() > 0.5f;
        _renderMg = true;
        
        if (_renderMg)
        {
            _renderMgAlt = GD.Randf() > 0.5f;
            if (!_renderMgAlt)
                _renderMgGlow = GD.Randf() > 0.5f;
        }
        
        // TODO: check if boss room for wall logic
        _renderWall = GD.Randi() % 5 == 4;
        _renderChains = _renderWall && GD.Randf() > 0.5f;
        _renderFg2 = GD.Randf() > 0.5f;
        
        // Pillar configuration
        if (_renderWall)
        {
            int roll = (int)(GD.Randi() % 3);
            _pillarConfig = roll switch
            {
                0 => PillarConfig.Open,
                1 => PillarConfig.Left1,
                _ => PillarConfig.Left2
            };
        }
        else
        {
            int roll = (int)(GD.Randi() % 3);
            _pillarConfig = roll switch
            {
                0 => PillarConfig.Open,
                1 => PillarConfig.SidesOnly,
                _ => PillarConfig.Full
            };
        }
        
        _renderThrone = false;
        
        UpdateVisibility();
    }
    
    private void UpdateVisibility()
    {
        _bg.Modulate = _overlayColor;
        _floor.Modulate = _overlayColor;
        _ceiling.Modulate = _overlayColor;
        _wall.Modulate = _overlayColor;
        _chains.Modulate = _overlayColor;
        _mg.Modulate = _overlayColor;
   
        _bg2.Visible = _renderAltBg;
        _bg2Glow.Visible = _renderAltBg;
        _bgGlow2.Visible = _darkDay;
        _bgGlow2Double.Visible = _darkDay;  // NEW: both layers show on dark days
        _bgGlow2Double.Modulate = new Color(1, 1, 1, 0.7f);  // Tweak this value to adjust brightness of dark day lights
        _wall.Visible = _renderWall;
        _chains.Visible = _renderChains;
        _chainsGlow.Visible = _renderChains;
        _chainsGlow2.Visible = _renderChains;  // NEW: both glow layers follow chains visibility
        _mg.Visible = _renderMg;
        _mgGlow.Visible = _renderMg;  // CHANGED: always visible when mg is visible
        _mgGlow2.Visible = _renderMg && _renderMgGlow;  // NEW: second layer only when animated
        _mgAlt.Visible = _renderMgAlt;  // RENAMED
        _mgAlt.Modulate = _renderMgGlow ? new Color(1f, 1f, 0.9f, 1f) : Colors.White; // TODO check if this line is really needed
        _fg2.Visible = _renderFg2;
        _throne.Visible = _renderThrone;
        _throneGlow.Visible = _renderThrone;
   
        // Pillars
        _pillar1.Visible = _pillarConfig is PillarConfig.SidesOnly or PillarConfig.Full or PillarConfig.Left1 or PillarConfig.Left2;
        _pillar2.Visible = _pillarConfig is PillarConfig.Full or PillarConfig.Left2;
        _pillar3.Visible = _pillarConfig == PillarConfig.Full;
        _pillar4.Visible = _pillarConfig == PillarConfig.Full;
        _pillar5.Visible = _pillarConfig is PillarConfig.SidesOnly or PillarConfig.Full;
    }
    
    private void UpdateFireFlies()
    {
        if (!_hasFlies) return;
        
        // Remove finished fireflies
        for (int i = _fireFlies.Count - 1; i >= 0; i--)
        {
            var fly = _fireFlies[i];
            if (fly.IsDone || !IsInstanceValid(fly))
            {
                if (IsInstanceValid(fly))
                    fly.QueueFree();
                _fireFlies.RemoveAt(i);
            }
        }
        
        // Spawn new fireflies (max 9, 10% chance per frame)
        if (_fireFlies.Count < 9 && GD.Randf() < 0.1f)
        {
            Color flyColor;
            if (_blueFlies)
            {
                flyColor = new Color(
                    (float)GD.RandRange(0.1, 0.2),
                    (float)GD.RandRange(0.6, 0.8),
                    (float)GD.RandRange(0.8, 1.0),
                    1.0f
                );
            }
            else
            {
                flyColor = new Color(
                    (float)GD.RandRange(0.8, 1.0),
                    (float)GD.RandRange(0.5, 0.8),
                    (float)GD.RandRange(0.3, 0.5),
                    1.0f
                );
            }
            
            var fly = FireFlyEffect.Create(flyColor);
            fly.ZIndex = -15;
            AddChild(fly);
            _fireFlies.Add(fly);
        }
    }
    
    private void UpdateGlowAnimations()
    {
        // Animated glow for chains (double rendered)
        if (_renderChains)
        {
            float chainsDegrees = (float)(Time.GetTicksMsec() % 360);
            float chainsAlpha = Mathf.Cos(Mathf.DegToRad(chainsDegrees)) / 10f + 0.9f;
            var chainsColor = new Color(1, 1, 1, chainsAlpha);
            _chainsGlow.Modulate = chainsColor;
            _chainsGlow2.Modulate = chainsColor;
        }
   
        // Glow for mg
        if (_renderMg)
        {
            if (_renderMgGlow)
            {
                float mgDegrees = (float)(Time.GetTicksMsec() / 10 % 360);
                float mgAlpha = Mathf.Cos(Mathf.DegToRad(mgDegrees)) / 2f + 0.5f;
                var mgColor = new Color(1, 1, 0.9f, mgAlpha);
                _mgGlow.Modulate = mgColor;
                _mgGlow2.Modulate = mgColor;
            }
            else
            {
                _mgGlow.Modulate = Colors.White;
            }
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
            var insertPoint = combatRoom.GetNodeOrNull<Control>("CombatVfxContainer");
            if (insertPoint != null)
            {
                ReparentToForeground(_fg, combatRoom, insertPoint);
                ReparentToForeground(_fgGlow, combatRoom, insertPoint);
                ReparentToForeground(_fg2, combatRoom, insertPoint);
            }

            var allyContainer = combatRoom.GetNodeOrNull<Control>("%AllyContainer");
            var enemyContainer = combatRoom.GetNodeOrNull<Control>("%EnemyContainer");
            if (allyContainer != null)
                allyContainer.Position += Vector2.Down * 30f;
            if (enemyContainer != null)
                enemyContainer.Position += Vector2.Down * 30f;
            if (LegacyActTracker.IsCollectorEncounter)
                SetBossMode(true);
        }
    }
    
    private void ReparentToForeground(TextureRect layer, NCombatRoom combatRoom, Node insertBefore)
    {
        var globalPos = layer.GlobalPosition;
        layer.GetParent().RemoveChild(layer);
        combatRoom.AddChild(layer);
        combatRoom.MoveChild(layer, insertBefore.GetIndex());
        layer.GlobalPosition = globalPos;
        layer.ZIndex = 0;
    }
    
    public void SetBossMode(bool isCollector)
    {
        _renderWall = false;
        _renderChains = false;
    
        // Re-roll pillars with non-wall options since wall is forced off
        int roll = (int)(GD.Randi() % 3);
        _pillarConfig = roll switch
        {
            0 => PillarConfig.Open,
            1 => PillarConfig.SidesOnly,
            _ => PillarConfig.Full
        };
    
        _renderThrone = isCollector;
        UpdateVisibility();
    }
    
}