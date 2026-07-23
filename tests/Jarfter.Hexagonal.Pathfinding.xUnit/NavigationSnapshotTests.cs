using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class NavigationSnapshotTests
{
    [Fact]
    public void HexNavigationCell_WhenUsingDefaultValue_ShouldRepresentNormalTraversableCell()
    {
        HexNavigationCell cell = default;

        Assert.Equal(1, cell.TraversalMultiplier);
        Assert.Equal(0, cell.ObstacleApothemScale);
        Assert.False(cell.HasObstacle);
    }

    [Fact]
    public void HexNavigationCell_WhenValuesAreValid_ShouldPreserveTerrainAndObstacleData()
    {
        HexNavigationCell cell = new HexNavigationCell(2, 0.8);

        Assert.Equal(2, cell.TraversalMultiplier);
        Assert.Equal(0.8, cell.ObstacleApothemScale);
        Assert.True(cell.HasObstacle);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexNavigationCell(0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexNavigationCell(1, -0.1));
    }

    [Fact]
    public void HexGridCentralNavigationSnapshot_WhenSourceMapChanges_ShouldKeepOriginalCells()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexagonalCubePoint point = new HexagonalCubePoint(1, -1);
        map[point] = new HexNavigationCell(2, 0.8);

        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 42);
        map[point] = new HexNavigationCell(3, 0.5);

        Assert.Equal(42, snapshot.Version);
        Assert.Equal(1, snapshot.Radius);
        Assert.Equal(7, snapshot.Count);
        Assert.Equal(0.8, snapshot.MaximumObstacleApothemScale);
        Assert.Equal(1, snapshot.MinimumTraversalMultiplier);
        Assert.True(snapshot.TryGetCell(point, out HexNavigationCell cell));
        Assert.Equal(2, cell.TraversalMultiplier);
        Assert.Equal(0.8, cell.ObstacleApothemScale);
    }

    [Fact]
    public void HexGridCentralNavigationSnapshot_WhenPointIsOutsideRadius_ShouldReportMissingCell()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        Assert.False(snapshot.TryGetCell(new HexagonalCubePoint(2, -1), out HexNavigationCell cell));
        Assert.Equal(default, cell);
    }

    [Fact]
    public void HexGridCentralNavigationMap_WhenCellChanges_ShouldCreateNewVersionedSnapshotWithoutMutatingOldSnapshot()
    {
        HexGridCentralNavigationMap map = new HexGridCentralNavigationMap(1);
        HexagonalCubePoint point = new HexagonalCubePoint(1, 0);
        HexGridCentralNavigationSnapshot firstSnapshot = map.CaptureSnapshot();

        bool changed = map.TrySetCell(point, new HexNavigationCell(2, 0.8));
        HexGridCentralNavigationSnapshot secondSnapshot = map.CaptureSnapshot();

        Assert.True(changed);
        Assert.Equal(0, firstSnapshot.Version);
        Assert.Equal(1, map.Version);
        Assert.Equal(1, secondSnapshot.Version);
        Assert.True(firstSnapshot.TryGetCell(point, out HexNavigationCell firstCell));
        Assert.True(secondSnapshot.TryGetCell(point, out HexNavigationCell secondCell));
        Assert.Equal(HexNavigationCell.Default, firstCell);
        Assert.Equal(new HexNavigationCell(2, 0.8), secondCell);
        Assert.False(map.TrySetCell(point, new HexNavigationCell(2, 0.8)));
        Assert.Equal(1, map.Version);
    }
}
