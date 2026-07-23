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
}
