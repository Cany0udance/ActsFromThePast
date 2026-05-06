using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ActsFromThePast.Minigames;

public partial class NPortalMapBuilderScreen : Control, IOverlayScreen, IScreenContext
{
    // ─── Colors ───
    private static readonly Color GoldColor = new(0.996078f, 0.819608f, 0f, 1f);
    private static readonly Color CreamColor = new(1f, 0.964706f, 0.886275f, 1f);
    private static readonly Color DarkPurple = new(0.0156863f, 0f, 0.156863f, 0.752941f);
    private static readonly Color OverBudgetColor = new(1f, 0.3f, 0.3f, 1f);
    private static readonly Color SelectedGlow = new(1f, 0.85f, 0.4f, 1f);
    private static readonly Color DimmedColor = new(0.85f, 0.8f, 0.75f, 1f);
    private static readonly Color PathDotColor = new(0.6f, 0.55f, 0.75f, 0.6f);
    private static readonly Color LockedXColor = new(0.8f, 0.2f, 0.2f, 0.7f);
 
    // ─── Localization ───
    private const string LocPrefix = "ACTSFROMTHEPAST-SECRET_PORTAL.minigame";
 
    // ─── Layout ───
    private const float NodeSpacing = 100f;
    private const float IconSize = 72f;
    private const float ArrowOffset = 60f;
    private const int PathDotCount = 2;
    private const float PathDotSize = 6f;
    private const float LegendIconSize = 40f;
 
    private static NPortalMapBuilderScreen? _instance;
    private PortalMapBuilderMinigame _minigame = null!;
    private readonly List<NodeSlot> _slots = new();
    private NProceedButton? _proceedButton;
    private bool _proceedButtonReady;
    private Button? _randomizeButton;
    private MegaRichTextLabel _budgetLabel = null!;
    private Tween? _fadeTween;
    private int _hoveredIndex = -1;
 
    private FontVariation? _fontBold;
    private FontVariation? _fontRegular;
    private readonly Dictionary<MapPointType, Texture2D?> _iconCache = new();
    private Texture2D? _xTexture;
 
    // Legend entries: type, localization key suffix, point cost
    private static readonly (MapPointType type, string locKey, int cost)[] LegendEntries =
    {
        (MapPointType.Unknown, "unknown", 1),
        (MapPointType.Shop, "merchant", 2),
        (MapPointType.Treasure, "treasure", 3),
        (MapPointType.RestSite, "restSite", 3),
        (MapPointType.Monster, "enemy", 1),
        (MapPointType.Elite, "elite", 2),
        (MapPointType.Unassigned, "empty", 0)
    };
 
    // ─── ShowScreen ───
 
    public static NPortalMapBuilderScreen ShowScreen(PortalMapBuilderMinigame minigame)
    {
        if (_instance != null && IsInstanceValid(_instance))
            _instance.QueueFree();
        var screen = new NPortalMapBuilderScreen();
        screen._minigame = minigame;
        screen.LoadResources();
        screen.BuildUI();
        screen.BindMinigameEvents();
        screen.RefreshAll();
        _instance = screen;
        NOverlayStack.Instance.Push((IOverlayScreen)screen);
        return screen;
    }
 
    private void BindMinigameEvents()
    {
        _minigame.SelectionChanged += OnSelectionChanged;
        _minigame.NodesChanged += OnNodesChanged;
        _minigame.Randomized += OnRandomized;
        _minigame.Finished += OnMinigameFinished;
    }
 
    private void UnbindMinigameEvents()
    {
        _minigame.SelectionChanged -= OnSelectionChanged;
        _minigame.NodesChanged -= OnNodesChanged;
        _minigame.Randomized -= OnRandomized;
        _minigame.Finished -= OnMinigameFinished;
    }
 
    // ─── Resources ───
 
    private void LoadResources()
    {
        _fontBold = GD.Load<FontVariation>("res://themes/kreon_bold_glyph_space_one.tres");
        _fontRegular = GD.Load<FontVariation>("res://themes/kreon_regular_shared.tres");
 
        foreach (var type in new[] {
            MapPointType.Monster, MapPointType.Elite, MapPointType.RestSite,
            MapPointType.Shop, MapPointType.Treasure, MapPointType.Unknown
        })
        {
            var path = GetIconPath(type);
            var tex = ResourceLoader.Load<Texture2D>(path);
            if (tex == null)
            {
                var fallback = ImageHelper.GetImagePath($"atlases/ui_atlas.sprites/map/icons/{GetIconName(type)}.tres");
                tex = ResourceLoader.Load<Texture2D>(fallback);
            }
            _iconCache[type] = tex;
        }
 
        _xTexture = GenerateXTexture(64, LockedXColor);
        _iconCache[MapPointType.Unassigned] = _xTexture;
    }
 
    private static ImageTexture GenerateXTexture(int size, Color color)
    {
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        int thickness = 5;
        int margin = size / 4;
        for (int i = margin; i < size - margin; i++)
        {
            float t = (float)(i - margin) / (size - 2 * margin - 1);
            int center1 = margin + (int)(t * (size - 2 * margin - 1));
            int center2 = (size - 1 - margin) - (int)(t * (size - 2 * margin - 1));
            for (int tt = -thickness; tt <= thickness; tt++)
            {
                int y1 = center1 + tt;
                if (y1 >= 0 && y1 < size)
                    image.SetPixel(i, y1, color);
                int y2 = center2 + tt;
                if (y2 >= 0 && y2 < size)
                    image.SetPixel(i, y2, color);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
 
    private static string GetIconPath(MapPointType type) => type switch
    {
        MapPointType.Monster => "res://images/atlases/ui_atlas.sprites/map/icons/map_monster.tres",
        MapPointType.Elite => "res://images/atlases/ui_atlas.sprites/map/icons/map_elite.tres",
        MapPointType.RestSite => "res://images/atlases/ui_atlas.sprites/map/icons/map_rest.tres",
        MapPointType.Shop => "res://images/atlases/ui_atlas.sprites/map/icons/map_shop.tres",
        MapPointType.Treasure => "res://images/atlases/ui_atlas.sprites/map/icons/map_chest.tres",
        MapPointType.Unknown => "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres",
        _ => "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres"
    };
 
    private static string GetIconName(MapPointType type) => type switch
    {
        MapPointType.Monster => "map_monster",
        MapPointType.Elite => "map_elite",
        MapPointType.RestSite => "map_rest",
        MapPointType.Shop => "map_shop",
        MapPointType.Treasure => "map_chest",
        MapPointType.Unknown => "map_unknown",
        _ => "map_unknown"
    };
 
    // ─── Lifecycle ───
 
    public override void _Ready()
    {

    }
 
    public override void _ExitTree()
    {
        UnbindMinigameEvents();
        _fadeTween?.Kill();
        _minigame.ForceEnd();
        _instance = null;
    }
 
    private void OnSelectionChanged()
    {
        RefreshAll();
    }
 
    private void OnNodesChanged()
    {
        RefreshAll();
    }
 
    private void OnRandomized()
    {
        if (_randomizeButton != null)
            _randomizeButton.Disabled = true;
        RefreshAll();
        // All nodes are now locked, move focus to proceed button
        _proceedButton?.GrabFocus();
    }
 
    // ─── IOverlayScreen / IScreenContext ───
 
    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => false;
 
    public Control DefaultFocusedControl
    {
        get
        {
            foreach (var slot in _slots)
                if (!slot.IsLocked) return slot.Icon;
            return (Control?)_proceedButton ?? this;
        }
    }
 
    public void AfterOverlayOpened()
    {
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(
            (GodotObject)this, (NodePath)"modulate:a",
            (Variant)1.0, 0.5
        ).From((Variant)0.0f);
    }
 
    public void AfterOverlayClosed()
    {
        _fadeTween?.Kill();
        this.QueueFreeSafely();
    }
 
    public void AfterOverlayShown() => UpdateProceedButton();
    public void AfterOverlayHidden() => UpdateProceedButton();
 
    // ─── UI Construction ───
 
    private void BuildUI()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Background
        var bg = new ColorRect { Color = new Color(0.14f, 0.06f, 0.32f, 1f) };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        var bgAccent = new ColorRect { Color = new Color(0.18f, 0.09f, 0.40f, 0.5f) };
        bgAccent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bgAccent);
        var nodesLayer = new Control();
        nodesLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        nodesLayer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(nodesLayer);
        float totalHeight = (_minigame.NodeCountTotal - 1) * NodeSpacing;
        float startY = -totalHeight / 2f;
        float cx = 0f;
        float anchorX = 0.5f;
        float anchorY = 0.5f;
        // Center panel behind node column
        var centerPanel = new NinePatchRect { Modulate = new Color(1f, 0.85f, 0.5f, 0.12f) };
        var centerNinePatchTex = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
        if (centerNinePatchTex != null)
        {
            centerPanel.Texture = centerNinePatchTex;
            centerPanel.RegionRect = new Rect2(0, 0, 48, 48);
            centerPanel.PatchMarginLeft = 14; centerPanel.PatchMarginTop = 14;
            centerPanel.PatchMarginRight = 13; centerPanel.PatchMarginBottom = 14;
        }
        centerPanel.AnchorLeft = anchorX; centerPanel.AnchorTop = anchorY;
        centerPanel.AnchorRight = anchorX; centerPanel.AnchorBottom = anchorY;
        centerPanel.OffsetLeft = -160; centerPanel.OffsetTop = startY - 80;
        centerPanel.OffsetRight = 160; centerPanel.OffsetBottom = startY + totalHeight + 50;
        centerPanel.GrowHorizontal = GrowDirection.Both;
        centerPanel.GrowVertical = GrowDirection.Both;
        centerPanel.MouseFilter = MouseFilterEnum.Ignore;
        nodesLayer.AddChild(centerPanel);
        // Node column
        for (int i = 0; i < _minigame.NodeCountTotal; i++)
        {
            float nodeY = startY + i * NodeSpacing;
            _slots.Add(CreateNodeSlot(i, nodesLayer, anchorX, anchorY, cx, nodeY));
            if (i < _minigame.NodeCountTotal - 1)
                CreatePathDots(nodesLayer, anchorX, anchorY, cx, nodeY, cx, nodeY + NodeSpacing);
        }
        // Title
        var titleLoc = new LocString("events", $"{LocPrefix}.title");
        var title = CreateStyledText($"[center]{titleLoc.GetFormattedText()}[/center]", _fontBold, 32, GoldColor);
        title.AnchorLeft = anchorX; title.AnchorTop = anchorY;
        title.AnchorRight = anchorX; title.AnchorBottom = anchorY;
        title.OffsetLeft = -150; title.OffsetTop = startY - 70;
        title.OffsetRight = 150; title.OffsetBottom = startY - 20;
        title.GrowHorizontal = GrowDirection.Both;
        title.GrowVertical = GrowDirection.Both;
        nodesLayer.AddChild(title);
        BuildLegendPanel(nodesLayer);
        BuildInstructionsPanel(nodesLayer);
        // Budget label
        _budgetLabel = CreateStyledText("", _fontBold, 32, CreamColor);
        _budgetLabel.AnchorTop = 1f; _budgetLabel.AnchorBottom = 1f;
        _budgetLabel.OffsetLeft = 64; _budgetLabel.OffsetTop = -89;
        _budgetLabel.OffsetRight = 770; _budgetLabel.OffsetBottom = -48;
        _budgetLabel.GrowVertical = GrowDirection.Begin;
        _budgetLabel.AddThemeConstantOverride("outline_size", 12);
        _budgetLabel.AddThemeColorOverride("font_outline_color", new Color(0.15f, 0.1f, 0.23f, 1f));
        _budgetLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
        _budgetLabel.AddThemeConstantOverride("shadow_offset_x", 5);
        _budgetLabel.AddThemeConstantOverride("shadow_offset_y", 4);
        AddChild(_budgetLabel);
        BuildProceedButton();
        BuildRandomizeButton();
        SetupNodeFocusNeighbors();
    }
 
    // ─── Node Slots ───
 
    private NodeSlot CreateNodeSlot(int index, Control parent, float anchorX, float anchorY, float cx, float cy)
    {
        float halfIcon = IconSize / 2f;
        int idx = index;
        bool locked = _minigame.IsLocked(index);
 
        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            AnchorLeft = anchorX, AnchorTop = anchorY,
            AnchorRight = anchorX, AnchorBottom = anchorY,
            OffsetLeft = cx - halfIcon, OffsetTop = cy - halfIcon,
            OffsetRight = cx + halfIcon, OffsetBottom = cy + halfIcon,
            GrowHorizontal = GrowDirection.Both, GrowVertical = GrowDirection.Both,
            PivotOffset = new Vector2(halfIcon, halfIcon),
            MouseFilter = locked ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop
        };
 
        var initialType = _minigame.GetNodeType(index);
        if (_iconCache.TryGetValue(initialType, out var initialTex) && initialTex != null)
            icon.Texture = initialTex;
 
        if (!locked)
        {
            icon.FocusMode = FocusModeEnum.All;
 
            icon.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(ev =>
                {
                    if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                    {
                        _minigame.SelectNode(idx);
                        icon.AcceptEvent();
                    }
                    else if (ev.IsActionPressed(MegaInput.select))
                    {
                        _minigame.SelectNode(idx);
                        icon.AcceptEvent();
                    }
                    else if (ev.IsActionPressed(MegaInput.left))
                    {
                        if (_minigame.SelectedIndex != idx)
                            _minigame.SelectNode(idx);
                        _minigame.CycleSelectedNode(-1);
                        icon.AcceptEvent();
                    }
                    else if (ev.IsActionPressed(MegaInput.right))
                    {
                        if (_minigame.SelectedIndex != idx)
                            _minigame.SelectNode(idx);
                        _minigame.CycleSelectedNode(1);
                        icon.AcceptEvent();
                    }
                }));
 
            icon.Connect(Control.SignalName.MouseEntered, Callable.From(() => SetHovered(idx)));
            icon.Connect(Control.SignalName.MouseExited, Callable.From(() => ClearHovered(idx)));
            icon.Connect(Control.SignalName.FocusEntered, Callable.From(() => SetHovered(idx)));
            icon.Connect(Control.SignalName.FocusExited, Callable.From(() => ClearHovered(idx)));
        }
        else
        {
            icon.SelfModulate = LockedXColor;
        }
 
        parent.AddChild(icon);
 
        Label? leftArrow = null;
        Label? rightArrow = null;
 
        if (!locked)
        {
            leftArrow = CreateArrowLabel("\u25C0", anchorX, anchorY,
                cx - halfIcon - ArrowOffset, cy - 16, cx - halfIcon - 10, cy + 16);
            leftArrow.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(ev =>
                {
                    if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                    {
                        _minigame.CycleSelectedNode(-1);
                        leftArrow.AcceptEvent();
                    }
                }));
            parent.AddChild(leftArrow);
 
            rightArrow = CreateArrowLabel("\u25B6", anchorX, anchorY,
                cx + halfIcon + 10, cy - 16, cx + halfIcon + ArrowOffset, cy + 16);
            rightArrow.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(ev =>
                {
                    if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                    {
                        _minigame.CycleSelectedNode(1);
                        rightArrow.AcceptEvent();
                    }
                }));
            parent.AddChild(rightArrow);
        }
 
        return new NodeSlot
        {
            Icon = icon,
            LeftArrow = leftArrow,
            RightArrow = rightArrow,
            Index = index,
            IsLocked = locked
        };
    }
 
    private Label CreateArrowLabel(string text, float anchorX, float anchorY,
        float left, float top, float right, float bottom)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            AnchorLeft = anchorX, AnchorTop = anchorY,
            AnchorRight = anchorX, AnchorBottom = anchorY,
            OffsetLeft = left, OffsetTop = top,
            OffsetRight = right, OffsetBottom = bottom,
            GrowHorizontal = GrowDirection.Both, GrowVertical = GrowDirection.Both
        };
        if (_fontBold != null)
            label.AddThemeFontOverride("font", _fontBold);
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", GoldColor);
        return label;
    }
 
    private void CreatePathDots(Control parent, float anchorX, float anchorY,
        float x1, float y1, float x2, float y2)
    {
        float halfDot = PathDotSize / 2f;
        float margin = IconSize / 4f;
        float my1 = y1 + margin;
        float my2 = y2 - margin;
        for (int i = 1; i <= PathDotCount; i++)
        {
            float t = (float)i / (PathDotCount + 1);
            float dx = Mathf.Lerp(x1, x2, t);
            float dy = Mathf.Lerp(my1, my2, t);
            parent.AddChild(new ColorRect
            {
                Color = PathDotColor,
                AnchorLeft = anchorX, AnchorTop = anchorY,
                AnchorRight = anchorX, AnchorBottom = anchorY,
                OffsetLeft = dx - halfDot, OffsetTop = dy - halfDot,
                OffsetRight = dx + halfDot, OffsetBottom = dy + halfDot,
                GrowHorizontal = GrowDirection.Both, GrowVertical = GrowDirection.Both,
                MouseFilter = MouseFilterEnum.Ignore
            });
        }
    }
 
    // ─── Controller Focus ───
 
    private void SetupNodeFocusNeighbors()
    {
        // Collect unlocked slot indices in order
        var unlocked = new List<int>();
        for (int i = 0; i < _slots.Count; i++)
            if (!_slots[i].IsLocked) unlocked.Add(i);
 
        for (int u = 0; u < unlocked.Count; u++)
        {
            var icon = _slots[unlocked[u]].Icon;
 
            // Up/down between unlocked nodes
            var above = u > 0 ? _slots[unlocked[u - 1]].Icon : icon;
            var below = u < unlocked.Count - 1 ? _slots[unlocked[u + 1]].Icon : icon;
 
            icon.FocusNeighborTop = above.GetPath();
            icon.FocusNeighborBottom = below.GetPath();
 
            // Left/right stay on self (intercepted by GuiInput for cycling)
            icon.FocusNeighborLeft = icon.GetPath();
            icon.FocusNeighborRight = icon.GetPath();
        }
 
        // Connect last unlocked node to randomize button
        if (unlocked.Count > 0 && _randomizeButton != null && !_randomizeButton.Disabled)
        {
            var lastIcon = _slots[unlocked[^1]].Icon;
            lastIcon.FocusNeighborBottom = _randomizeButton.GetPath();
            _randomizeButton.FocusNeighborTop = lastIcon.GetPath();
            _randomizeButton.FocusNeighborBottom = _randomizeButton.GetPath();
            _randomizeButton.FocusNeighborLeft = _randomizeButton.GetPath();
            _randomizeButton.FocusNeighborRight = _randomizeButton.GetPath();
        }
    }
 
    // ─── Legend Panel (left side) ───
 
    private void BuildLegendPanel(Control parent)
    {
        var legendPanel = new Control
        {
            AnchorLeft = 0f, AnchorTop = 0.5f,
            AnchorRight = 0f, AnchorBottom = 0.5f,
            OffsetLeft = 48, OffsetTop = -220,
            OffsetRight = 340, OffsetBottom = 200,
            GrowHorizontal = GrowDirection.End,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore
        };
        parent.AddChild(legendPanel);
 
        var panelBg = new NinePatchRect { Modulate = DarkPurple };
        var ninePatchTex = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
        if (ninePatchTex != null)
        {
            panelBg.Texture = ninePatchTex;
            panelBg.RegionRect = new Rect2(0, 0, 48, 48);
            panelBg.PatchMarginLeft = 14; panelBg.PatchMarginTop = 14;
            panelBg.PatchMarginRight = 13; panelBg.PatchMarginBottom = 14;
        }
        panelBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        legendPanel.AddChild(panelBg);
 
        var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 20; vbox.OffsetTop = 16;
        vbox.OffsetRight = -20; vbox.OffsetBottom = -16;
        vbox.AddThemeConstantOverride("separation", 6);
        legendPanel.AddChild(vbox);
 
        var legendTitleLoc = new LocString("events", $"{LocPrefix}.legend.title");
        vbox.AddChild(CreateStyledText($"[center]{legendTitleLoc.GetFormattedText()}[/center]", _fontBold, 26, GoldColor));
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
 
        foreach (var (type, locKey, cost) in LegendEntries)
        {
            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(0, 40)
            };
            row.AddThemeConstantOverride("separation", 12);
 
            // Icon
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(LegendIconSize, LegendIconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            if (_iconCache.TryGetValue(type, out var tex) && tex != null)
                icon.Texture = tex;
            if (type == MapPointType.Unassigned)
                icon.SelfModulate = LockedXColor;
            row.AddChild(icon);
 
            // Name
            var nameLoc = new LocString("events", $"{LocPrefix}.legend.{locKey}");
            var nameLabel = new Label
            {
                Text = nameLoc.GetFormattedText(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            if (_fontRegular != null)
                nameLabel.AddThemeFontOverride("font", _fontRegular);
            nameLabel.AddThemeFontSizeOverride("font_size", 22);
            nameLabel.AddThemeColorOverride("font_color", type == MapPointType.Unassigned ? LockedXColor : CreamColor);
            row.AddChild(nameLabel);
 
            // Cost
            string costText;
            if (cost > 0)
            {
                var costLoc = new LocString("events", $"{LocPrefix}.legend.cost");
                costLoc.Add("Cost", (decimal)cost);
                costText = costLoc.GetFormattedText();
            }
            else
            {
                costText = new LocString("events", $"{LocPrefix}.legend.costFree").GetFormattedText();
            }
 
            var costLabel = new Label
            {
                Text = costText,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                CustomMinimumSize = new Vector2(50, 0),
                MouseFilter = MouseFilterEnum.Ignore
            };
            if (_fontBold != null)
                costLabel.AddThemeFontOverride("font", _fontBold);
            costLabel.AddThemeFontSizeOverride("font_size", 22);
            costLabel.AddThemeColorOverride("font_color", type == MapPointType.Unassigned ? LockedXColor : GoldColor);
            row.AddChild(costLabel);
 
            vbox.AddChild(row);
        }
    }
 
    // ─── Instructions Panel (right side) ───
 
    private void BuildInstructionsPanel(Control parent)
    {
        var rightPanel = new Control
        {
            AnchorLeft = 1f, AnchorTop = 0.5f,
            AnchorRight = 1f, AnchorBottom = 0.5f,
            OffsetLeft = -400, OffsetTop = -340,
            OffsetRight = -48, OffsetBottom = 260,
            GrowHorizontal = GrowDirection.Begin,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore
        };
        parent.AddChild(rightPanel);
 
        var panelBg = new NinePatchRect { Modulate = DarkPurple };
        var ninePatchTex = GD.Load<Texture2D>("res://images/ui/tiny_nine_patch.png");
        if (ninePatchTex != null)
        {
            panelBg.Texture = ninePatchTex;
            panelBg.RegionRect = new Rect2(0, 0, 48, 48);
            panelBg.PatchMarginLeft = 14; panelBg.PatchMarginTop = 14;
            panelBg.PatchMarginRight = 13; panelBg.PatchMarginBottom = 14;
        }
        panelBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rightPanel.AddChild(panelBg);
 
        var textVBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        textVBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        textVBox.OffsetLeft = 28; textVBox.OffsetTop = 20;
        textVBox.OffsetRight = -28; textVBox.OffsetBottom = -20;
        textVBox.AddThemeConstantOverride("separation", 20);
        rightPanel.AddChild(textVBox);
 
        var instrTitleLoc = new LocString("events", $"{LocPrefix}.instructions.title");
        textVBox.AddChild(CreateStyledText(
            $"[center]{instrTitleLoc.GetFormattedText()}[/center]", _fontBold, 28, GoldColor));
 
        var instrBodyLoc = new LocString("events", $"{LocPrefix}.instructions.description");
        instrBodyLoc.Add("Budget", (decimal)_minigame.MaxBudget);
        var body = CreateStyledText(
            instrBodyLoc.GetFormattedText(),
            _fontRegular, 22, CreamColor);
        body.CustomMinimumSize = new Vector2(0, 280);
        body.AddThemeConstantOverride("line_separation", -4);
        textVBox.AddChild(body);
    }
 
    // ─── Proceed Button ───
 
    private void BuildProceedButton()
    {
        var proceedScene = GD.Load<PackedScene>("res://scenes/ui/proceed_button.tscn");
        if (proceedScene == null) return;
 
        _proceedButton = proceedScene.Instantiate<NProceedButton>();
        _proceedButton.Visible = true;
        AddChild(_proceedButton);
 
        Callable.From(() =>
        {
            _proceedButton.UpdateText(NProceedButton.ProceedLoc);
            _proceedButton.Disable();
            _proceedButtonReady = true;
            UpdateProceedButton();
        }).CallDeferred();
 
        _proceedButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(new Action<NButton>(_ =>
            {
                _minigame.Confirm();
            }))
        );
    }
 
    // ─── Randomize Button ───
 
    private void BuildRandomizeButton()
    {
        _randomizeButton = new Button
        {
            Text = new LocString("events", $"{LocPrefix}.randomize").GetFormattedText(),
            AnchorTop = 1f, AnchorBottom = 1f,
            AnchorLeft = 0.5f, AnchorRight = 0.5f,
            OffsetLeft = -90, OffsetTop = -95,
            OffsetRight = 90, OffsetBottom = -48,
            GrowVertical = GrowDirection.Begin,
            GrowHorizontal = GrowDirection.Both,
            Disabled = _minigame.IsRandomized,
            FocusMode = FocusModeEnum.All
        };
 
        if (_fontBold != null)
            _randomizeButton.AddThemeFontOverride("font", _fontBold);
        _randomizeButton.AddThemeFontSizeOverride("font_size", 24);
 
        _randomizeButton.Pressed += () =>
        {
            _minigame.Randomize();
        };
 
        _randomizeButton.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(ev =>
            {
                if (ev.IsActionPressed(MegaInput.select) && !_randomizeButton.Disabled)
                {
                    _randomizeButton.AcceptEvent();
                    _minigame.Randomize();
                }
            }));
 
        AddChild(_randomizeButton);
    }
 
    // ─── Styled Text Helper ───
 
    private MegaRichTextLabel CreateStyledText(string bbcode, FontVariation? font, int fontSize, Color color)
    {
        var label = new MegaRichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            AutoSizeEnabled = false
        };
        label.AddThemeColorOverride("default_color", color);
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_font_size", fontSize);
        label.AddThemeFontSizeOverride("italics_font_size", fontSize);
        if (font != null)
            label.AddThemeFontOverride("normal_font", font);
        label.Text = bbcode;
        return label;
    }
 
    // ─── Hover ───
 
    private void SetHovered(int index)
    {
        int prev = _hoveredIndex;
        _hoveredIndex = index;
        if (prev >= 0 && prev < _slots.Count) ApplySlotColor(prev);
        ApplySlotColor(index);
    }
 
    private void ClearHovered(int index)
    {
        if (_hoveredIndex != index) return;
        _hoveredIndex = -1;
        ApplySlotColor(index);
    }
 
    // ─── Input ───
 
    private void OnMinigameFinished()
    {
        NOverlayStack.Instance.Remove((IOverlayScreen)this);
    }
 
    // ─── Refresh ───
 
    private void RefreshAll()
    {
        int totalCost = _minigame.TotalCost;
        int budget = _minigame.MaxBudget;
        bool overBudget = _minigame.IsOverBudget;
 
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
 
            if (slot.IsLocked) continue;
 
            var nodeType = _minigame.GetNodeType(i);
            bool isSelected = i == _minigame.SelectedIndex;
            bool lockedNow = _minigame.IsLocked(i);
 
            if (_iconCache.TryGetValue(nodeType, out var tex))
                slot.Icon.Texture = tex;
 
            if (slot.LeftArrow != null) slot.LeftArrow.Visible = isSelected && !lockedNow;
            if (slot.RightArrow != null) slot.RightArrow.Visible = isSelected && !lockedNow;
 
            if (lockedNow)
                slot.Icon.MouseFilter = MouseFilterEnum.Ignore;
 
            ApplySlotColor(i);
        }
 
        if (overBudget)
        {
            var budgetLoc = new LocString("events", $"{LocPrefix}.budgetOver");
            budgetLoc.Add("Current", (decimal)totalCost);
            budgetLoc.Add("Max", (decimal)budget);
            _budgetLabel.Text = budgetLoc.GetFormattedText();
        }
        else
        {
            var budgetLoc = new LocString("events", $"{LocPrefix}.budget");
            budgetLoc.Add("Current", (decimal)totalCost);
            budgetLoc.Add("Max", (decimal)budget);
            _budgetLabel.Text = budgetLoc.GetFormattedText();
        }
 
        UpdateProceedButton();
    }
 
    private void ApplySlotColor(int i)
    {
        if (i < 0 || i >= _slots.Count) return;
        var slot = _slots[i];
        if (slot.IsLocked) return;
 
        bool isSelected = i == _minigame.SelectedIndex;
        bool overBudget = _minigame.IsOverBudget && !_minigame.IsRandomized;
 
        Color color;
        if (overBudget)
            color = OverBudgetColor;
        else if (isSelected)
            color = SelectedGlow;
        else if (i == _hoveredIndex)
            color = Colors.White;
        else
            color = DimmedColor;
 
        slot.Icon.SelfModulate = color;
 
        if (isSelected)
            slot.Icon.Scale = Vector2.One * 1.2f;
        else if (i == _hoveredIndex)
            slot.Icon.Scale = Vector2.One * 1.1f;
        else
            slot.Icon.Scale = Vector2.One;
    }
 
    private void UpdateProceedButton()
    {
        if (_proceedButton == null || !_proceedButtonReady) return;
        if (_minigame.IsValid)
            _proceedButton.Enable();
        else
            _proceedButton.Disable();
    }
 
    // ─── Data ───
 
    private class NodeSlot
    {
        public TextureRect Icon { get; init; } = null!;
        public Label? LeftArrow { get; init; }
        public Label? RightArrow { get; init; }
        public int Index { get; init; }
        public bool IsLocked { get; init; }
    }
}
 
