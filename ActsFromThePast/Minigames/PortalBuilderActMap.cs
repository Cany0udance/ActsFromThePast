using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Minigames;

public sealed class PortalBuilderActMap : ActMap
{
    private const int Width = 7;
    private const int Middle = 3;
 
    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    protected override MapPoint?[,] Grid { get; }
 
    /// <summary>
    /// Coordinates in the new map that should be marked as visited.
    /// Does NOT include the starting point (it's not normally in visited coords).
    /// </summary>
    public IReadOnlyList<MapCoord> NewVisitedCoords { get; }
 
    public PortalBuilderActMap(RunState runState, MapPointType[] chosenNodes, int availableNodeCount)
    {
        var visitedCoords = runState.VisitedMapCoords;
        var originalMap = runState.Map;
        var origStart = originalMap.StartingMapPoint;
 
        // ─── First pass: collect types to determine grid size ───
 
        var visitedTypes = new List<MapPointType>();
        foreach (var coord in visitedCoords)
        {
            if (coord.col == origStart.coord.col && coord.row == origStart.coord.row)
                continue;
 
            var origPoint = originalMap.GetPoint(coord);
            if (origPoint != null)
                visitedTypes.Add(origPoint.PointType);
        }
 
        var activeNodes = new List<MapPointType>();
        for (int i = 0; i < chosenNodes.Length && i < availableNodeCount; i++)
        {
            if (chosenNodes[i] != MapPointType.Unassigned)
                activeNodes.Add(chosenNodes[i]);
        }
 
        int totalNodes = visitedTypes.Count + activeNodes.Count;
        int height = totalNodes + 1;
        int visitedCount = visitedTypes.Count;
        int activeCount = activeNodes.Count;
 
        Log.Info($"[PortalBuilderActMap] Building: {visitedCount} visited + {activeCount} new = {totalNodes} total, grid {Width}x{height}");
 
        // ─── Build map ───
 
        Grid = new MapPoint?[Width, height];
 
        StartingMapPoint = new MapPoint(Middle, 0)
        {
            PointType = origStart.PointType
        };
 
        BossMapPoint = new MapPoint(Middle, height)
        {
            PointType = MapPointType.Boss
        };
 
        MapPoint? previous = StartingMapPoint;
        var newVisited = new List<MapCoord>();
        int row = 1;
 
        // Place visited nodes at rows 1..visitedCount
        foreach (var nodeType in visitedTypes)
        {
            var point = new MapPoint(Middle, row) { PointType = nodeType };
            Grid[Middle, row] = point;
 
            previous.AddChildPoint(point);
            if (startMapPoints.Count == 0)
                startMapPoints.Add(point);
 
            newVisited.Add(new MapCoord(Middle, row));
            previous = point;
            row++;
        }
        
// Place minigame nodes after visited (reversed: bottom of minigame = first after visited)
        for (int i = activeNodes.Count - 1; i >= 0; i--)
        {
            if (row >= height) break;

            var point = new MapPoint(Middle, row) { PointType = activeNodes[i] };
            Grid[Middle, row] = point;

            previous.AddChildPoint(point);
            if (startMapPoints.Count == 0)
                startMapPoints.Add(point);

            previous = point;
            row++;
        }
 
        // Last node connects to boss
        previous.AddChildPoint(BossMapPoint);
 
        NewVisitedCoords = newVisited;
 
        Log.Info($"[PortalBuilderActMap] Built: start -> {visitedCount} visited -> {activeCount} new -> boss. {newVisited.Count} visited coords to restore.");
    }
}
