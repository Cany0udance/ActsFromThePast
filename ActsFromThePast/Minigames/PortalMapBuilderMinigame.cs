using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class PortalMapBuilderMinigame
{
    private const int NodeCount = 6;
    private const int Budget = 11;

    /// <summary>
    /// Types the player can cycle through.
    /// </summary>
    private static readonly MapPointType[] AvailableTypes =
    {
        MapPointType.Monster,
        MapPointType.Elite,
        MapPointType.RestSite,
        MapPointType.Shop,
        MapPointType.Treasure,
        MapPointType.Unknown,
        MapPointType.Unassigned
    };

    /// <summary>
    /// Types that can appear when randomizing (excludes Unassigned/Empty).
    /// </summary>
    private static readonly MapPointType[] RandomizableTypes =
    {
        MapPointType.Monster,
        MapPointType.Elite,
        MapPointType.RestSite,
        MapPointType.Shop,
        MapPointType.Treasure,
        MapPointType.Unknown
    };

    private static readonly Dictionary<MapPointType, int> PointCosts = new()
    {
        { MapPointType.Monster, 1 },
        { MapPointType.Elite, 2 },
        { MapPointType.RestSite, 3 },
        { MapPointType.Shop, 2 },
        { MapPointType.Treasure, 3 },
        { MapPointType.Unknown, 1 },
        { MapPointType.Unassigned, 0 }
    };

    private readonly TaskCompletionSource _completionSource = new();
    private readonly Player _owner;
    private int _selectedIndex = -1;

    public MapPointType[] Nodes { get; }
    public Rng Rng { get; }
    public int MaxBudget => Budget;

    /// <summary>
    /// How many actual map nodes are available between the player and the boss.
    /// Slots beyond this count (from the bottom) are locked as Unassigned (X).
    /// </summary>
    public int AvailableNodeCount { get; }

    /// <summary>
    /// Whether the player has used the one-time randomize.
    /// When true, all slots are locked and budget is ignored.
    /// </summary>
    public bool IsRandomized { get; private set; }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex == value) return;
            int old = _selectedIndex;
            _selectedIndex = value;
            Log.Info($"[MapBuilder] SelectionChanged: {old} -> {_selectedIndex}");
            SelectionChanged?.Invoke();
        }
    }

    public int TotalCost
    {
        get
        {
            int total = 0;
            foreach (var type in Nodes)
                total += GetCost(type);
            return total;
        }
    }

    public bool IsOverBudget => TotalCost > Budget;
    public bool IsValid => IsRandomized || !IsOverBudget;

    public event Action? SelectionChanged;
    public event Action? NodesChanged;
    public event Action? Randomized;
    public event Action? Finished;

    public PortalMapBuilderMinigame(Player owner, Rng rng, int availableNodeCount)
    {
        _owner = owner;
        Rng = rng;
        AvailableNodeCount = Math.Clamp(availableNodeCount, 0, NodeCount);
        Nodes = new MapPointType[NodeCount];

        // Fill available slots with Monster, locked slots with Unassigned (X)
        for (int i = 0; i < NodeCount; i++)
            Nodes[i] = i < AvailableNodeCount ? MapPointType.Monster : MapPointType.Unassigned;

        Log.Info($"[MapBuilder] Created. Available: {AvailableNodeCount}/{NodeCount}, Budget: {Budget}, Starting cost: {TotalCost}");
    }

    /// <summary>
    /// Returns true if the slot is locked and cannot be changed.
    /// Locked slots fill from the bottom up, and all slots lock after randomize.
    /// </summary>
    public bool IsLocked(int index) => index >= AvailableNodeCount || IsRandomized;

    public static int GetCost(MapPointType type)
    {
        return PointCosts.TryGetValue(type, out int cost) ? cost : 1;
    }

    public void CycleSelectedNode(int direction)
    {
        if (_selectedIndex < 0 || _selectedIndex >= NodeCount)
        {
            Log.Info($"[MapBuilder] CycleSelectedNode ignored: selectedIndex={_selectedIndex}");
            return;
        }

        if (IsLocked(_selectedIndex))
        {
            Log.Info($"[MapBuilder] CycleSelectedNode ignored: slot {_selectedIndex} is locked");
            return;
        }

        var oldType = Nodes[_selectedIndex];
        int idx = Array.IndexOf(AvailableTypes, oldType);
        idx = (idx + direction + AvailableTypes.Length) % AvailableTypes.Length;
        Nodes[_selectedIndex] = AvailableTypes[idx];

        Log.Info($"[MapBuilder] CycleSelectedNode: slot {_selectedIndex}, {oldType}({GetCost(oldType)}) -> {Nodes[_selectedIndex]}({GetCost(Nodes[_selectedIndex])}), total cost: {TotalCost}/{Budget}");
        NodesChanged?.Invoke();
    }

    public void SelectNode(int index)
    {
        if (IsLocked(index))
        {
            Log.Info($"[MapBuilder] SelectNode ignored: slot {index} is locked");
            return;
        }

        Log.Info($"[MapBuilder] SelectNode: index={index}, current={_selectedIndex}");
        SelectedIndex = (index == _selectedIndex) ? -1 : index;
    }

    /// <summary>
    /// One-time randomize: fills all available slots with random non-Empty types,
    /// locks everything, and bypasses the budget check.
    /// </summary>
    public void Randomize()
    {
        if (IsRandomized) return;

        for (int i = 0; i < AvailableNodeCount; i++)
        {
            int roll = Rng.NextInt(0, RandomizableTypes.Length);
            Nodes[i] = RandomizableTypes[roll];
        }

        IsRandomized = true;
        _selectedIndex = -1;
        Log.Info($"[MapBuilder] Randomized! Final cost: {TotalCost} (budget ignored)");
        Randomized?.Invoke();
        NodesChanged?.Invoke();
    }

    public void Confirm()
    {
        if (!IsValid)
        {
            Log.Info("[MapBuilder] Confirm rejected: over budget");
            return;
        }
        if (_completionSource.Task.IsCompleted)
        {
            Log.Info("[MapBuilder] Confirm rejected: already completed");
            return;
        }
        Log.Info($"[MapBuilder] Confirmed! Final cost: {TotalCost}/{Budget}");
        _completionSource.SetResult();
        Finished?.Invoke();
    }

    public void ForceEnd()
    {
        if (_completionSource.Task.IsCompleted) return;
        Log.Info("[MapBuilder] ForceEnd");
        _completionSource.SetCanceled();
    }

    public async Task PlayMinigame()
    {
        if (!LocalContext.IsMe(_owner)) return;
        Log.Info("[MapBuilder] PlayMinigame: showing screen");
        NPortalMapBuilderScreen.ShowScreen(this);
        await _completionSource.Task;
        Log.Info("[MapBuilder] PlayMinigame: completed");
    }

    public MapPointType GetNodeType(int index) => Nodes[index];
    public int NodeCountTotal => NodeCount;
}