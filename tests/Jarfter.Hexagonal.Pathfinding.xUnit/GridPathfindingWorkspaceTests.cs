using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridPathfindingWorkspaceTests
{
    [Fact]
    public void CaptureSnapshot_WhenMapIsUnchangedOrChanged_ShouldReuseBakedTopology()
    {
        HexGridCentralNavigationMap map = new HexGridCentralNavigationMap(3);
        HexGridCentralNavigationSnapshot firstSnapshot = map.CaptureSnapshot();

        map.TrySetCell(new HexagonalCubePoint(1, 0), new HexNavigationCell(2, 0.5));
        HexGridCentralNavigationSnapshot secondSnapshot = map.CaptureSnapshot();

        Assert.Same(firstSnapshot.Bake, secondSnapshot.Bake);
        Assert.Equal(37, firstSnapshot.Bake.Count);
        Assert.True(firstSnapshot.Bake.TryGetIndex(new HexagonalCubePoint(1, 0), out int index));
        Assert.Equal(new HexagonalCubePoint(1, 0), firstSnapshot.Bake.GetPoint(index));
        Assert.True(firstSnapshot.Bake.TryGetIndex(new HexagonalCubePoint(3, 0), out int boundaryIndex));
        Assert.Equal(-1, firstSnapshot.Bake.GetNeighborIndex(boundaryIndex, 0));
    }

    [Fact]
    public void FindPath_WhenUsingReusableWorkspace_ShouldMatchStatelessAlgorithmsAcrossSnapshots()
    {
        HexGridCentralNavigationMap map = new HexGridCentralNavigationMap(3);
        map.TrySetCell(new HexagonalCubePoint(1, 0), new HexNavigationCell(1, 1));
        HexGridCentralNavigationSnapshot firstSnapshot = map.CaptureSnapshot();
        HexGridPathfindingWorkspace workspace = new HexGridPathfindingWorkspace(firstSnapshot.Bake);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        HexagonalCubePoint goal = new HexagonalCubePoint(3, 0);

        HexGridPath? expectedAStarPath = HexGridAStar.Instance.FindPath(
            firstSnapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);
        HexGridPath? workspaceAStarPath = HexGridAStar.Instance.FindPath(
            firstSnapshot,
            workspace,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);
        HexGridPath? expectedThetaStarPath = HexGridThetaStar.Instance.FindPath(
            firstSnapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);
        HexGridPath? workspaceThetaStarPath = HexGridThetaStar.Instance.FindPath(
            firstSnapshot,
            workspace,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);

        AssertEquivalent(expectedAStarPath, workspaceAStarPath);
        AssertEquivalent(expectedThetaStarPath, workspaceThetaStarPath);

        map.TrySetCell(new HexagonalCubePoint(2, 0), new HexNavigationCell(1, 1));
        HexGridCentralNavigationSnapshot secondSnapshot = map.CaptureSnapshot();
        HexGridPath? expectedSecondPath = HexGridThetaStar.Instance.FindPath(
            secondSnapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);
        HexGridPath? workspaceSecondPath = HexGridThetaStar.Instance.FindPath(
            secondSnapshot,
            workspace,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);

        AssertEquivalent(expectedSecondPath, workspaceSecondPath);
    }

    [Fact]
    public void FindPath_WhenWorkspaceUsesDifferentBake_ShouldThrow()
    {
        HexGridCentralNavigationMap firstMap = new HexGridCentralNavigationMap(2);
        HexGridCentralNavigationMap secondMap = new HexGridCentralNavigationMap(2);
        HexGridPathfindingWorkspace workspace = new HexGridPathfindingWorkspace(firstMap.CaptureSnapshot().Bake);

        Assert.Throws<ArgumentException>(() => HexGridAStar.Instance.FindPath(
            secondMap.CaptureSnapshot(),
            workspace,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25)));
    }

    private static void AssertEquivalent(HexGridPath? expected, HexGridPath? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.Points.ToArray(), actual.Points.ToArray());
        Assert.Equal(expected.Cost, actual.Cost, 12);
        Assert.Equal(expected.NavigationVersion, actual.NavigationVersion);
    }
}
