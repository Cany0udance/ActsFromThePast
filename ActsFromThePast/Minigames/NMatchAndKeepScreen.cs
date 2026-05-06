using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ActsFromThePast.Minigames;

public partial class NMatchAndKeepScreen : Control, IOverlayScreen, IScreenContext
{
    // ─── Scale ───
    private const float GridScale = 0.5f;
    private const float SelectedScale = 0.6f;
    private const float MismatchScale = 0.75f;
    private const float ScaleTweenTime = 0.2f;
 
    // ─── Grid Layout (from STS1 at scale=1.0) ───
    // STS1: col * 210 + 640 for X, row * -230 + 750 for Y (LibGDX Y-up)
    // Godot: center-relative, Y-down
    // STS1 center = (960, 540). Col X values: 640,850,1060,1270 → offsets from center: -320,-110,100,310
    // Row Y values (LibGDX): 750,520,290 → from center: +210,-20,-250 → Godot (flip Y): -210,+20,+250
    private static readonly float[] ColOffsets = { -320f, -110f, 100f, 310f };
    private static readonly float[] RowOffsets = { -210f, 20f, 250f };
 
    // ─── Timing (from STS1 logs) ───
    private const float MatchWait = 1.0f;
    private const float MismatchWait = 1.25f;
    private const float GameDoneWait = 1.0f;
    private const float CleanupWait = 1.0f;
    private const float CardSlideTime = 0.4f;
    private const float FadeInDuration = 0.5f;
    private const float FadeOutDuration = 0.5f;
 
    // ─── Card Back ───
    private const string CardBackAtlasPath = "res://images/event_extras/cardui.atlas";
    private const string CardBackRegionName = "512/card_back";
 
    // ─── Event Background ───
    private const string EventRegionName = "event";
    private const string LocPrefix = "ACTSFROMTHEPAST-MATCH_AND_KEEP.minigame";
 
    private static NMatchAndKeepScreen? _instance;
    private MatchAndKeepMinigame _minigame = null!;
 
    // ─── UI Elements ───
    private Control _particleContainer = null!;
    private Control _gridContainer = null!;
    private MegaRichTextLabel _attemptsLabel = null!;
    private readonly List<CardSlot> _slots = new();
 
    // ─── Game State ───
    private int _firstSelection = -1;
    private int _attemptsLeft;
    private int _matchCount;
    private bool _isProcessing;
 
    // ─── Tweens ───
    private Tween? _waitTween;
    private Tween? _spawnTween;
 
    // ─── Card Back Texture ───
    private AtlasTexture? _cardBackTexture;
 
    // ─── ShowScreen ───
 
    public static NMatchAndKeepScreen ShowScreen(MatchAndKeepMinigame minigame)
    {
        if (_instance != null && IsInstanceValid(_instance))
            _instance.QueueFree();
 
        var screen = new NMatchAndKeepScreen();
        screen._minigame = minigame;
        screen._attemptsLeft = minigame.MaxAttempts;
        screen.LoadCardBack();
        screen.BuildUI();
        _instance = screen;
        NOverlayStack.Instance.Push((IOverlayScreen)screen);
        screen.DealCards();
        
        return screen;
    }
 
    // ─── Lifecycle ───
 
    public override void _ExitTree()
    {
        KillAllTweens();
        _minigame.ForceEnd();
        _instance = null;
    }
 
    private void KillAllTweens()
    {
        _waitTween?.Kill();
        _spawnTween?.Kill();
    }
 
    // ─── IOverlayScreen / IScreenContext ───
 
    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => false;
    public Control DefaultFocusedControl
    {
        get
        {
            foreach (var slot in _slots)
                if (!slot.IsMatched) return slot.Holder;
            return this;
        }
    }
 
    public void AfterOverlayOpened() { }
    public void AfterOverlayClosed()
    {
        KillAllTweens();
        this.QueueFreeSafely();
    }
    public void AfterOverlayShown() { }
    public void AfterOverlayHidden() { }
 
    // ─── Resources ───
 
    private void LoadCardBack()
    {
        var region = LibGdxAtlas.GetRegion(CardBackAtlasPath, CardBackRegionName);
        if (region != null)
        {
            _cardBackTexture = new AtlasTexture();
            _cardBackTexture.Atlas = region.Value.Texture;
            _cardBackTexture.Region = region.Value.Region;
        }
    }
 
    private static string GetEventAtlasPath(int actIndex) => actIndex switch
    {
        0 => "res://ActsFromThePast/backgrounds/exordium/scene.atlas",
        1 => "res://ActsFromThePast/backgrounds/city/scene.atlas",
        _ => "res://ActsFromThePast/backgrounds/beyond/scene.atlas"
    };
 
    // ─── UI Construction ───
 
    private void BuildUI()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
 
        // Event background
        var region = LibGdxAtlas.GetRegion(GetEventAtlasPath(_minigame.ActIndex), EventRegionName);
        if (region != null)
        {
            var atlasTex = new AtlasTexture();
            atlasTex.Atlas = region.Value.Texture;
            atlasTex.Region = region.Value.Region;
 
            var bgRect = new TextureRect
            {
                Texture = atlasTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            bgRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(bgRect);
        }
 
        // Particle layer
        _particleContainer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _particleContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_particleContainer);
 
        // Grid container
        _gridContainer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _gridContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_gridContainer);
 
        // Create 12 card slots
        for (int i = 0; i < 12; i++)
            CreateCardSlot(i);
 
        // Controller navigation
        SetupFocusNeighbors();
 
        // Attempts label
        var font = GD.Load<FontVariation>("res://themes/kreon_bold_glyph_space_one.tres");
        _attemptsLabel = new MegaRichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Off,
            AnchorLeft = 0.5f, AnchorTop = 1f,
            AnchorRight = 0.5f, AnchorBottom = 1f,
            OffsetLeft = -200, OffsetTop = -80,
            OffsetRight = 200, OffsetBottom = -30,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Begin
        };
        _attemptsLabel.AddThemeColorOverride("default_color", Colors.White);
        _attemptsLabel.AddThemeFontSizeOverride("normal_font_size", 30);
        if (font != null)
            _attemptsLabel.AddThemeFontOverride("normal_font", font);
        _attemptsLabel.AddThemeConstantOverride("outline_size", 10);
        _attemptsLabel.AddThemeColorOverride("font_outline_color", new Color(0.15f, 0.1f, 0.23f, 1f));
        AddChild(_attemptsLabel);
        RefreshAttemptsLabel();
 
        StartParticleSpawner();
 
        // Start transparent for fade-in
        Modulate = new Color(1f, 1f, 1f, 0f);
    }
 
    private void CreateCardSlot(int index)
    {
        int col = index % 4;
        int row = index % 3;
 
        // Wrapper at center of screen, will be tweened to grid position
        var wrapper = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = 0f, OffsetTop = 0f,
            OffsetRight = 0f, OffsetBottom = 0f,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            Scale = new Vector2(GridScale, GridScale)
        };
        _gridContainer.AddChild(wrapper);
 
        // Card holder
        var cardModel = _minigame.Cards[index];
        var ncard = NCard.Create(cardModel, ModelVisibility.Visible);
        var holder = NGridCardHolder.Create(ncard);
        holder.Position = Vector2.Zero;
        wrapper.AddChild(holder);
 
        // Hide card face until flipped
        ncard.Visible = false;
 
        // UpdateVisuals deferred so _Ready has run
        Callable.From(() =>
        {
            ncard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        }).CallDeferred();
 
        // Card back overlay
        TextureRect? overlay = null;
        if (_cardBackTexture != null)
        {
            overlay = new TextureRect
            {
                Texture = _cardBackTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(300, 422),
                Position = new Vector2(-150, -211),
                Size = new Vector2(300, 422),
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = true
            };
            holder.AddChild(overlay);
        }
 
        // Click
        int idx = index;
        holder.Pressed += _ => OnCardClicked(idx);
 
        // Suppress hover tips on face-down cards
        holder.Connect(Control.SignalName.MouseEntered, Callable.From(() =>
        {
            if (idx < _slots.Count && !_slots[idx].IsFaceUp)
                Callable.From(() => NHoverTipSet.Remove(holder)).CallDeferred();
        }));
        holder.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
        {
            if (idx < _slots.Count && !_slots[idx].IsFaceUp)
                Callable.From(() => NHoverTipSet.Remove(holder)).CallDeferred();
        }));
 
        // Start below screen center (STS1: y=-300, which is below screen in LibGDX)
        // In Godot Y-down, below screen = large positive Y offset
        wrapper.OffsetTop = 800f;
        wrapper.OffsetBottom = 800f;
 
        _slots.Add(new CardSlot
        {
            Wrapper = wrapper,
            Holder = holder,
            CardNode = ncard,
            Overlay = overlay,
            PairIndex = _minigame.PairIndices[index],
            IsFaceUp = false,
            IsMatched = false,
            Col = col,
            Row = row
        });
    }
 
    // ─── Controller Focus ───
 
    private void SetupFocusNeighbors()
    {
        // Build grid map: (col, row) → slot index, skipping matched cards
        var gridMap = new int[4, 3];
        for (int c = 0; c < 4; c++)
            for (int r = 0; r < 3; r++)
                gridMap[c, r] = -1;
 
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].IsMatched) continue;
            gridMap[_slots[i].Col, _slots[i].Row] = i;
        }
 
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsMatched) continue;
 
            var holder = slot.Holder;
            int col = slot.Col;
            int row = slot.Row;
 
            holder.FocusNeighborLeft = FindNeighbor(gridMap, col, row, -1, 0)?.GetPath() ?? holder.GetPath();
            holder.FocusNeighborRight = FindNeighbor(gridMap, col, row, 1, 0)?.GetPath() ?? holder.GetPath();
            holder.FocusNeighborTop = FindNeighbor(gridMap, col, row, 0, -1)?.GetPath() ?? holder.GetPath();
            holder.FocusNeighborBottom = FindNeighbor(gridMap, col, row, 0, 1)?.GetPath() ?? holder.GetPath();
        }
    }
 
    private NGridCardHolder? FindNeighbor(int[,] gridMap, int col, int row, int dCol, int dRow)
    {
        int c = col + dCol;
        int r = row + dRow;
 
        // Wrap until we find a non-matched slot or return to start
        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (c < 0) c = 3;
            if (c > 3) c = 0;
            if (r < 0) r = 2;
            if (r > 2) r = 0;
 
            int idx = gridMap[c, r];
            if (idx >= 0) return _slots[idx].Holder;
 
            c += dCol;
            r += dRow;
        }
        return null;
    }
 
    // ─── Deal Cards ───
 
    private void DealCards()
    {
        var tween = CreateTween();
        tween.SetParallel(true);
 
        // Fade in
        tween.TweenProperty(
            (GodotObject)this, (NodePath)"modulate:a",
            (Variant)1.0f, FadeInDuration
        ).From((Variant)0.0f)
         .SetTrans(Tween.TransitionType.Cubic)
         .SetEase(Tween.EaseType.Out);
 
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            float targetX = ColOffsets[slot.Col];
            float targetY = RowOffsets[slot.Row];
 
            tween.TweenProperty(
                (GodotObject)slot.Wrapper, (NodePath)"offset_left",
                (Variant)targetX, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
 
            tween.TweenProperty(
                (GodotObject)slot.Wrapper, (NodePath)"offset_right",
                (Variant)targetX, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
 
            tween.TweenProperty(
                (GodotObject)slot.Wrapper, (NodePath)"offset_top",
                (Variant)targetY, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
 
            tween.TweenProperty(
                (GodotObject)slot.Wrapper, (NodePath)"offset_bottom",
                (Variant)targetY, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
 
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() =>
        {
        }));
    }
 
    // ─── Card Click ───
 
    private void OnCardClicked(int index)
    {
        if (_isProcessing) return;
 
        var slot = _slots[index];
        if (slot.IsFaceUp || slot.IsMatched) return;
 
        // Reveal: show card face, hide card back, set scale to selected
        slot.IsFaceUp = true;
        slot.CardNode.Visible = true;
        if (slot.Overlay != null)
            slot.Overlay.Visible = false;
        SetSlotScale(index, SelectedScale);
 
        if (_firstSelection < 0)
        {
            // First card
            _firstSelection = index;
        }
        else
        {
            // Second card — determine match or mismatch
            int first = _firstSelection;
            _firstSelection = -1;
            _isProcessing = true;
 
            bool isMatch = _minigame.Cards[first].Id == _minigame.Cards[index].Id;
 
            if (isMatch)
            {
                HandleMatch(first, index);
            }
            else
            {
                HandleMismatch(first, index);
            }
        }
    }
 
    // ─── Match ───
 
    private void HandleMatch(int a, int b)
    {
 
        // Both stay at SelectedScale (0.7), slide to center
        var tween = CreateTween();
        tween.SetParallel(true);
 
        foreach (int idx in new[] { a, b })
        {
            tween.TweenProperty(
                (GodotObject)_slots[idx].Wrapper, (NodePath)"offset_left",
                (Variant)0f, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
 
            tween.TweenProperty(
                (GodotObject)_slots[idx].Wrapper, (NodePath)"offset_right",
                (Variant)0f, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
 
            tween.TweenProperty(
                (GodotObject)_slots[idx].Wrapper, (NodePath)"offset_top",
                (Variant)0f, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
 
            tween.TweenProperty(
                (GodotObject)_slots[idx].Wrapper, (NodePath)"offset_bottom",
                (Variant)0f, CardSlideTime
            ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        }
 
        tween.SetParallel(false);
 
        // Wait at center
        tween.TweenInterval(MatchWait);
 
        // Resolve
        tween.TweenCallback(Callable.From(() =>
        {
            _slots[a].IsMatched = true;
            _slots[b].IsMatched = true;
            _slots[a].Wrapper.Visible = false;
            _slots[b].Wrapper.Visible = false;
 
            // Add matched card to deck with preview
            var canonical = _minigame.Canonicals[_slots[a].PairIndex];
            var player = _minigame.Owner;
            async Task AddMatchedCard()
            {
                var cardInstance = player.RunState.CreateCard(canonical, player);
                var result = await CardPileCmd.Add(cardInstance, PileType.Deck);
                CardCmd.PreviewCardPileAdd(result);
            }
            TaskHelper.RunSafely(AddMatchedCard());
 
            _matchCount++;
            _attemptsLeft--;
            RefreshAttemptsLabel();
            SetupFocusNeighbors();
            _isProcessing = false;
            
            CheckGameEnd();
        }));
    }
 
    // ─── Mismatch ───
 
    private void HandleMismatch(int a, int b)
    {
 
        // Both scale up to MismatchScale (1.0)
        SetSlotScale(a, MismatchScale);
        SetSlotScale(b, MismatchScale);
 
        // Wait, then flip back and shrink
        _waitTween?.Kill();
        _waitTween = CreateTween();
        _waitTween.TweenInterval(MismatchWait);
        _waitTween.TweenCallback(Callable.From(() =>
        {
            // Flip back: hide card face, show card back
            _slots[a].IsFaceUp = false;
            _slots[b].IsFaceUp = false;
            _slots[a].CardNode.Visible = false;
            _slots[b].CardNode.Visible = false;
            if (_slots[a].Overlay != null) _slots[a].Overlay.Visible = true;
            if (_slots[b].Overlay != null) _slots[b].Overlay.Visible = true;
 
            // Scale back to grid
            SetSlotScale(a, GridScale);
            SetSlotScale(b, GridScale);
 
            _attemptsLeft--;
            RefreshAttemptsLabel();
            _isProcessing = false;
            
            CheckGameEnd();
        }));
    }
 
    // ─── Scale Helper ───
 
    private void SetSlotScale(int index, float scale)
    {
        var wrapper = _slots[index].Wrapper;
        var tween = wrapper.CreateTween();
        tween.TweenProperty(
            (GodotObject)wrapper, (NodePath)"scale",
            (Variant)new Vector2(scale, scale), ScaleTweenTime
        ).SetTrans(Tween.TransitionType.Cubic)
         .SetEase(Tween.EaseType.Out);
    }
 
    // ─── Game End ───
 
    private void CheckGameEnd()
    {
        bool allMatched = _matchCount >= 6;
        bool outOfAttempts = _attemptsLeft <= 0;
 
        if (!allMatched && !outOfAttempts) return;
        
        _isProcessing = true;
 
        // Wait, then clean up
        _waitTween?.Kill();
        _waitTween = CreateTween();
 
        if (outOfAttempts && !allMatched)
            _waitTween.TweenInterval(GameDoneWait);
 
        // Slide remaining cards to center-below (STS1: target_x=WIDTH/2, target_y=-300)
        _waitTween.TweenCallback(Callable.From(() =>
        {
            var cleanupTween = CreateTween();
            cleanupTween.SetParallel(true);
 
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsMatched) continue;
 
                // Scale back to grid
                SetSlotScale(i, GridScale);
 
                cleanupTween.TweenProperty(
                    (GodotObject)slot.Wrapper, (NodePath)"offset_left",
                    (Variant)0f, CardSlideTime
                ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
 
                cleanupTween.TweenProperty(
                    (GodotObject)slot.Wrapper, (NodePath)"offset_right",
                    (Variant)0f, CardSlideTime
                ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
 
                cleanupTween.TweenProperty(
                    (GodotObject)slot.Wrapper, (NodePath)"offset_top",
                    (Variant)800f, CardSlideTime
                ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
 
                cleanupTween.TweenProperty(
                    (GodotObject)slot.Wrapper, (NodePath)"offset_bottom",
                    (Variant)800f, CardSlideTime
                ).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            }
 
            cleanupTween.SetParallel(false);
            cleanupTween.TweenInterval(CleanupWait);
 
            // Fade out
            cleanupTween.TweenProperty(
                (GodotObject)this, (NodePath)"modulate:a",
                (Variant)0.0f, FadeOutDuration
            ).SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.In);
 
            cleanupTween.TweenCallback(Callable.From(() =>
            {
                _spawnTween?.Kill();
                _minigame.Complete();
                NOverlayStack.Instance.Remove((IOverlayScreen)this);
            }));
        }));
    }
 
    // ─── Attempts Label ───
 
    private void RefreshAttemptsLabel()
    {
        var loc = new LocString("events", $"{LocPrefix}.attempts");
        loc.Add("Count", (decimal)_attemptsLeft);
        _attemptsLabel.Text = $"[center]{loc.GetFormattedText()}[/center]";
    }
 
    // ─── Particle Spawner ───
 
    private void StartParticleSpawner()
    {
        _spawnTween?.Kill();
        _spawnTween = CreateTween();
        _spawnTween.SetLoops();
        _spawnTween.TweenCallback(Callable.From(SpawnParticle));
        _spawnTween.TweenInterval(1.0f);
    }
 
    private void SpawnParticle()
    {
        var size = Size;
        if (size.X <= 0 || size.Y <= 0) return;
        var particle = EventBgParticleEffect.Create(size / 2f);
        _particleContainer.AddChild(particle);
    }
 
    // ─── Data ───
 
    private class CardSlot
    {
        public Control Wrapper { get; init; } = null!;
        public NGridCardHolder Holder { get; init; } = null!;
        public NCard CardNode { get; init; } = null!;
        public TextureRect? Overlay { get; init; }
        public int PairIndex { get; init; }
        public int Col { get; init; }
        public int Row { get; init; }
        public bool IsFaceUp { get; set; }
        public bool IsMatched { get; set; }
    }
}
 
