using Jarfter.NavMesh.Build;
using Jarfter.NavMesh.Dynamic;
using Jarfter.NavMesh.Crowd;
using Jarfter.NavMesh.xUnit.ComparisonTest;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using Jarfter.NavMesh.Serialization;
using Jarfter.NavMesh.Topology;
using Jarfter.NavMesh.Tiles;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.xUnit;

public sealed class NavMeshTests
{
    [Fact]
    public void FindPath_WhenConnectedTriangles_ShouldReturnDirectPath()
    {
        NavMeshPoint[] vertices =
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        NavMeshTriangle[] triangles = [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)];
        Mesh navMesh = Mesh.Create(vertices, triangles);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.2, 0.1), new NavMeshPoint(0.1, 0.8));

        Assert.NotNull(path);
        Assert.Equal([0, 1], path.Corridor);
        Assert.Equal([new NavMeshPoint(0.2, 0.1), new NavMeshPoint(0.1, 0.8)], path.Points);
        Assert.True(path.TotalCost > 0);
    }

    [Fact]
    public void FindPath_WhenDisconnectedTriangles_ShouldReturnNull()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1), new NavMeshPoint(3, 0),
            new NavMeshPoint(4, 0), new NavMeshPoint(3, 1)
        ];
        NavMeshTriangle[] triangles = [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(3, 4, 5)];
        Mesh navMesh = Mesh.Create(vertices, triangles);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.1, 0.1), new NavMeshPoint(3.1, 0.1));

        Assert.Null(path);
    }

    [Fact]
    public void TryFindCorridor_WhenDestinationHasCapacity_ShouldReturnUnsmoothedPathAndCost()
    {
        NavMeshPoint[] vertices =
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        Mesh navMesh = Mesh.Create(vertices, [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
        Span<int> corridor = stackalloc int[2];

        bool found = navMesh.TryFindCorridor(new NavMeshPoint(0.8, 0.2), new NavMeshPoint(0.2, 0.8),
            navMesh.CreateQueryWorkspace(), NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, corridor,
            out int corridorCount, out double totalCost);

        Assert.True(found);
        Assert.Equal(2, corridorCount);
        Assert.Equal([0, 1], corridor);
        Assert.True(totalCost > 0);
    }

    [Fact]
    public void FindPath_WhenWorkspaceIsReused_ShouldKeepIndependentResults()
    {
        NavMeshPoint[] vertices =
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        Mesh navMesh = Mesh.Create(vertices, [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
        NavMeshQueryWorkspace workspace = navMesh.CreateQueryWorkspace();

        NavMeshPath? first = navMesh.FindPath(new NavMeshPoint(0.2, 0.1), new NavMeshPoint(0.1, 0.8), workspace);
        NavMeshPath? second = navMesh.FindPath(new NavMeshPoint(0.1, 0.8), new NavMeshPoint(0.2, 0.1), workspace);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(new NavMeshPoint(0.2, 0.1), first.Points[0]);
        Assert.Equal(new NavMeshPoint(0.1, 0.8), second.Points[0]);
    }

    [Fact]
    public void FindPath_WhenCorridorTurnsAroundCorner_ShouldKeepCornerWaypoint()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1), new NavMeshPoint(1, 1),
            new NavMeshPoint(1, 3), new NavMeshPoint(0, 3)
        ];
        NavMeshTriangle[] triangles =
        [
            new NavMeshTriangle(0, 1, 3), new NavMeshTriangle(1, 2, 3), new NavMeshTriangle(0, 3, 5),
            new NavMeshTriangle(3, 4, 5)
        ];
        Mesh navMesh = Mesh.Create(vertices, triangles);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(2.5, 0.5), new NavMeshPoint(0.5, 2.5));

        Assert.NotNull(path);
        Assert.Contains(new NavMeshPoint(1, 1), path.Points);
        Assert.Equal(new NavMeshPoint(2.5, 0.5), path.Points[0]);
        Assert.Equal(new NavMeshPoint(0.5, 2.5), path.Points[^1]);
    }

    [Fact]
    public void Create_WhenTriangleIsClockwise_ShouldThrow()
    {
        NavMeshPoint[] vertices = [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1)];

        Assert.Throws<ArgumentException>(() => Mesh.Create(vertices, [new NavMeshTriangle(0, 2, 1)]));
    }

    [Fact]
    public void Create_WhenThreeTrianglesShareEdge_ShouldThrow()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1), new NavMeshPoint(0, -1),
            new NavMeshPoint(0.5, 2)
        ];

        Assert.Throws<ArgumentException>(() =>
            Mesh.Create(vertices,
                [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(1, 0, 3), new NavMeshTriangle(0, 1, 4)]));
    }
}

public sealed class NavMeshBuildValidatorTests
{
    [Fact]
    public void Normalize_WhenObstacleIsOutsideBoundary_ShouldThrow()
    {
        NavMeshPolygon boundary = new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 4), new NavMeshPoint(0, 4)
        ]);
        NavMeshPolygon obstacle = new NavMeshPolygon([
            new NavMeshPoint(5, 1), new NavMeshPoint(6, 1), new NavMeshPoint(6, 2), new NavMeshPoint(5, 2)
        ]);

        Assert.Throws<ArgumentException>(() =>
            NavMeshBuildValidator.Normalize(new NavMeshBuildInput(boundary, [obstacle])));
    }

    [Fact]
    public void Normalize_WhenObstaclesOverlap_ShouldThrow()
    {
        NavMeshPolygon boundary = new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(8, 0), new NavMeshPoint(8, 8), new NavMeshPoint(0, 8)
        ]);
        NavMeshPolygon first = new NavMeshPolygon([
            new NavMeshPoint(1, 1), new NavMeshPoint(4, 1), new NavMeshPoint(4, 4), new NavMeshPoint(1, 4)
        ]);
        NavMeshPolygon second = new NavMeshPolygon([
            new NavMeshPoint(3, 3), new NavMeshPoint(6, 3), new NavMeshPoint(6, 6), new NavMeshPoint(3, 6)
        ]);

        Assert.Throws<ArgumentException>(() =>
            NavMeshBuildValidator.Normalize(new NavMeshBuildInput(boundary, [first, second])));
    }

    [Fact]
    public void Normalize_WhenBoundarySelfIntersects_ShouldThrow()
    {
        NavMeshPolygon boundary = new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(4, 4), new NavMeshPoint(0, 4), new NavMeshPoint(4, 0)
        ]);

        Assert.Throws<ArgumentException>(() => NavMeshBuildValidator.Normalize(new NavMeshBuildInput(boundary)));
    }
}

public sealed class NavMeshBuilderTests
{
    [Fact]
    public void Build_WhenAreaAndFlagsAreConfigured_ShouldApplyThemToEveryTriangle()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)
        ]));

        Mesh navMesh = NavMeshBuilder.Build(input, new NavMeshBuildOptions { AreaId = 7, Flags = 0b_0101 });

        for (int index = 0; index < navMesh.TriangleCount; index++)
        {
            Assert.Equal(7, navMesh.GetAreaId(index));
            Assert.Equal(0b_0101u, navMesh.GetFlags(index));
        }
    }

    [Fact]
    public void Build_WhenAgentRadiusIsPositive_ShouldShrinkWalkableSpaceAndExpandObstacle()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(
            new NavMeshPolygon([
                new NavMeshPoint(0, 0), new NavMeshPoint(10, 0), new NavMeshPoint(10, 10), new NavMeshPoint(0, 10)
            ]),
            [
                new NavMeshPolygon([
                    new NavMeshPoint(4, 4), new NavMeshPoint(6, 4), new NavMeshPoint(6, 6), new NavMeshPoint(4, 6)
                ])
            ]);

        Mesh navMesh = NavMeshBuilder.Build(input, new NavMeshBuildOptions { AgentRadius = 1 });

        Assert.Null(navMesh.FindPath(new NavMeshPoint(0.5, 5), new NavMeshPoint(2, 5)));
        Assert.Null(navMesh.FindPath(new NavMeshPoint(3.5, 5), new NavMeshPoint(2, 5)));
        Assert.NotNull(navMesh.FindPath(new NavMeshPoint(2, 2), new NavMeshPoint(8, 2)));
    }

    [Fact]
    public void Build_WhenAgentRadiusAndBoundaryIsConcave_ShouldThrow()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1), new NavMeshPoint(1, 1),
            new NavMeshPoint(1, 3), new NavMeshPoint(0, 3)
        ]));

        Assert.Throws<NotSupportedException>(() =>
            NavMeshBuilder.Build(input, new NavMeshBuildOptions { AgentRadius = 0.25 }));
    }

    [Fact]
    public void Build_WhenBoundaryIsConcave_ShouldProduceQueryableMesh()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1), new NavMeshPoint(1, 1),
            new NavMeshPoint(1, 3), new NavMeshPoint(0, 3)
        ]));

        Mesh navMesh = NavMeshBuilder.Build(input);
        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(2.5, 0.5), new NavMeshPoint(0.5, 2.5));

        Assert.Equal(4, navMesh.TriangleCount);
        Assert.NotNull(path);
        Assert.Contains(new NavMeshPoint(1, 1), path.Points);
    }

    [Fact]
    public void Build_WhenInputContainsObstacle_ShouldKeepObstacleUnwalkable()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(
            new NavMeshPolygon([
                new NavMeshPoint(0, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 4), new NavMeshPoint(0, 4)
            ]),
            [
                new NavMeshPolygon([
                    new NavMeshPoint(1, 1), new NavMeshPoint(2, 1), new NavMeshPoint(2, 2), new NavMeshPoint(1, 2)
                ])
            ]);

        Mesh navMesh = NavMeshBuilder.Build(input);
        NavMeshPoint start = new NavMeshPoint(0.5, 1.5);
        NavMeshPoint goal = new NavMeshPoint(3.5, 1.5);
        NavMeshPath? path = navMesh.FindPath(start, goal);

        Assert.Null(navMesh.FindPath(new NavMeshPoint(1.5, 1.5), new NavMeshPoint(3.5, 3.5)));
        Assert.True(navMesh.TryRaycastBoundary(start, goal, out _));
        Assert.NotNull(path);
        Assert.True(path.Points.Count > 2);
    }

    [Fact]
    public void Build_WhenInputContainsMultipleObstacles_ShouldKeepEachObstacleUnwalkable()
    {
        NavMeshBuildInput input = new NavMeshBuildInput(
            new NavMeshPolygon([
                new NavMeshPoint(0, 0), new NavMeshPoint(8, 0), new NavMeshPoint(8, 4), new NavMeshPoint(0, 4)
            ]),
            [
                new NavMeshPolygon([
                    new NavMeshPoint(1, 1), new NavMeshPoint(2, 1), new NavMeshPoint(2, 3), new NavMeshPoint(1, 3)
                ]),
                new NavMeshPolygon([
                    new NavMeshPoint(5, 1), new NavMeshPoint(6, 1), new NavMeshPoint(6, 3), new NavMeshPoint(5, 3)
                ])
            ]);

        Mesh navMesh = NavMeshBuilder.Build(input);

        Assert.Null(navMesh.FindPath(new NavMeshPoint(1.5, 2), new NavMeshPoint(4, 2)));
        Assert.Null(navMesh.FindPath(new NavMeshPoint(5.5, 2), new NavMeshPoint(4, 2)));
        Assert.NotNull(navMesh.FindPath(new NavMeshPoint(3, 2), new NavMeshPoint(4, 2)));
    }
}

public sealed class NavMeshQueryFilterTests
{
    [Fact]
    public void FindPath_WhenGoalAreaIsFilteredOut_ShouldReturnNull()
    {
        NavMeshPoint[] vertices =
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        Mesh navMesh = Mesh.Create(vertices, [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3, 7)]);
        NavMeshQueryWorkspace workspace = navMesh.CreateQueryWorkspace();

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.8, 0.2), new NavMeshPoint(0.1, 0.8), workspace,
            new ExcludingFilter(7), NavMeshQueryDefaults.CostPolicy);

        Assert.Null(path);
    }

    private sealed class ExcludingFilter(int excludedAreaId) : INavMeshQueryFilter
    {
        public bool Pass(int triangleIndex, int areaId, uint flags) => areaId != excludedAreaId;
    }
}

public sealed class NavMeshFlagsTests
{
    [Fact]
    public void FindPath_WhenGoalFlagsAreExcluded_ShouldReturnNull()
    {
        NavMeshPoint[] vertices =
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        Mesh navMesh = Mesh.Create(vertices,
            [new NavMeshTriangle(0, 1, 2, 0, 0b_0001), new NavMeshTriangle(0, 2, 3, 0, 0b_0010)]);
        NavMeshQueryFilter filter = new NavMeshQueryFilter { IncludedFlags = 0b_0001 };

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.8, 0.2), new NavMeshPoint(0.1, 0.8),
            navMesh.CreateQueryWorkspace(), filter, NavMeshQueryDefaults.CostPolicy);

        Assert.Null(path);
        Assert.Equal(0b_0001u, navMesh.GetFlags(0));
    }
}

public sealed class NavMeshConvexPolygonTests
{
    [Fact]
    public void Create_WhenUsingConvexPolygon_ShouldPreserveLogicalPolygonCount()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(3, 1), new NavMeshPoint(2, 2),
            new NavMeshPoint(0, 2), new NavMeshPoint(-1, 1)
        ];
        NavMeshConvexPolygon polygon = new NavMeshConvexPolygon([0, 1, 2, 3, 4, 5], 7, 0b_0011);

        Mesh navMesh = Mesh.Create(vertices, [polygon]);
        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0, 1), new NavMeshPoint(2, 1));

        Assert.Equal(1, navMesh.PolygonCount);
        Assert.Equal(4, navMesh.TriangleCount);
        Assert.Equal(7, navMesh.GetAreaId(0));
        Assert.NotNull(path);
        Assert.Equal([new NavMeshPoint(0, 1), new NavMeshPoint(2, 1)], path.Points);
    }

    [Fact]
    public void FindPath_WhenConvexPolygonsShareEdge_ShouldUseOneCorridorNodePerPolygon()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(2, 0), new NavMeshPoint(2, 1)
        ];
        Mesh navMesh = Mesh.Create(vertices,
            [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([1, 4, 5, 2])]);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.25, 0.5), new NavMeshPoint(1.75, 0.5));

        Assert.NotNull(path);
        Assert.Equal(4, navMesh.TriangleCount);
        Assert.Equal([0, 1], path.Corridor);
    }

    [Fact]
    public void FindPath_WhenUsingCachedLocation_ShouldSkipCoordinateLookupAndKeepCorridor()
    {
        Mesh navMesh = Mesh.Create(
            [
                new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
                new NavMeshPoint(2, 0), new NavMeshPoint(2, 1)
            ],
            [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([1, 4, 5, 2])]);
        NavMeshQueryWorkspace workspace = navMesh.CreateQueryWorkspace();

        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.25, 0.5), out NavMeshLocation start));
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(1.75, 0.5), out NavMeshLocation goal));
        NavMeshPath? path = navMesh.FindPath(start, goal, workspace, NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy);

        Assert.True(navMesh.IsValidPolygonRef(start.PolygonRef));
        Assert.NotNull(path);
        Assert.Equal([0, 1], path.Corridor);
        Assert.False(navMesh.IsValidPolygonRef(NavMeshPolygonRef.Invalid));
    }

    [Fact]
    public void FindPath_WhenCachedLocationBelongsToAnotherMesh_ShouldReturnNull()
    {
        NavMeshPoint[] vertices =
        [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)];
        NavMeshConvexPolygon[] polygons = [new NavMeshConvexPolygon([0, 1, 2, 3])];
        Mesh source = Mesh.Create(vertices, polygons);
        Mesh target = Mesh.Create(vertices, polygons);

        Assert.True(source.TryFindLocation(new NavMeshPoint(0.25, 0.5), out NavMeshLocation start));
        Assert.True(source.TryFindLocation(new NavMeshPoint(0.75, 0.5), out NavMeshLocation goal));

        Assert.False(target.IsValidPolygonRef(start.PolygonRef));
        Assert.Null(target.FindPath(start, goal, target.CreateQueryWorkspace(), NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy));
    }

    [Fact]
    public void TryProjectToPolygon_WhenPointIsOutsideKnownPolygon_ShouldProjectWithoutGlobalLookup()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)],
            [new NavMeshConvexPolygon([0, 1, 2, 3])]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation source));

        bool projected = navMesh.TryProjectToPolygon(source.PolygonRef, new NavMeshPoint(1.5, 0.25),
            out NavMeshLocation location);

        Assert.True(projected);
        Assert.Equal(source.PolygonRef, location.PolygonRef);
        Assert.Equal(new NavMeshPoint(1, 0.25), location.Position);
    }

    [Fact]
    public void TryProjectToPolygonBoundary_WhenPointIsInsideKnownPolygon_ShouldReturnNearestBoundary()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)],
            [new NavMeshConvexPolygon([0, 1, 2, 3])]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation source));

        bool projected = navMesh.TryProjectToPolygonBoundary(source.PolygonRef, new NavMeshPoint(0.2, 0.5),
            out NavMeshLocation location);

        Assert.True(projected);
        Assert.Equal(source.PolygonRef, location.PolygonRef);
        Assert.Equal(new NavMeshPoint(0, 0.5), location.Position);
    }
}

public sealed class NavMeshJumpConnectionTests
{
    [Fact]
    public void FindPath_WhenDisconnectedPolygonsHaveJump_ShouldUseJumpAndIncludeFixedCost()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(4, 0), new NavMeshPoint(6, 0), new NavMeshPoint(6, 1), new NavMeshPoint(4, 1)
        ];
        NavMeshConvexPolygon[] polygons =
        [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([4, 5, 6, 7])];
        NavMeshJumpConnection jump = new NavMeshJumpConnection(new NavMeshPoint(1.5, 0.25),
            new NavMeshPoint(4.5, 0.25), 5);
        Mesh navMesh = Mesh.Create(vertices, polygons, [jump]);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(1.1, 0.25), new NavMeshPoint(5.8, 0.25));

        Assert.NotNull(path);
        Assert.Single(path.Jumps);
        Assert.Equal(jump.FixedCost, path.Jumps[0].FixedCost);
        Assert.Equal(6.7, path.TotalCost, 10);
        Assert.Equal([new NavMeshPoint(1.1, 0.25), jump.Start, jump.End, new NavMeshPoint(5.8, 0.25)], path.Points);
    }

    [Fact]
    public void FindPath_WhenJumpIsOneWay_ShouldNotReachReverseDirection()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1)
        ];
        Mesh navMesh = Mesh.Create(vertices,
            [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([4, 5, 6, 7])],
            [new NavMeshJumpConnection(new NavMeshPoint(0.75, 0.1), new NavMeshPoint(3.75, 0.1), 1)]);

        Assert.NotNull(navMesh.FindPath(new NavMeshPoint(0.6, 0.1), new NavMeshPoint(3.9, 0.1)));
        Assert.Null(navMesh.FindPath(new NavMeshPoint(3.9, 0.1), new NavMeshPoint(0.6, 0.1)));
    }

    [Fact]
    public void FindPath_WhenChainedJumpsCostLessThanDirectJump_ShouldUseOptimalJumpSequence()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1),
            new NavMeshPoint(6, 0), new NavMeshPoint(7, 0), new NavMeshPoint(7, 1), new NavMeshPoint(6, 1)
        ];
        NavMeshJumpConnection firstJump = new NavMeshJumpConnection(new NavMeshPoint(0.75, 0.5),
            new NavMeshPoint(3.25, 0.5), 1);
        NavMeshJumpConnection secondJump = new NavMeshJumpConnection(new NavMeshPoint(3.75, 0.5),
            new NavMeshPoint(6.25, 0.5), 1);
        NavMeshJumpConnection directJump = new NavMeshJumpConnection(new NavMeshPoint(0.8, 0.5),
            new NavMeshPoint(6.2, 0.5), 10);
        Mesh navMesh = Mesh.Create(vertices,
        [
            new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([4, 5, 6, 7]),
            new NavMeshConvexPolygon([8, 9, 10, 11])
        ], [firstJump, secondJump, directJump]);

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.5, 0.5), new NavMeshPoint(6.5, 0.5));

        Assert.NotNull(path);
        Assert.Equal([0, 1], path.Jumps.Select(static jump => jump.ConnectionIndex));
        Assert.Equal(firstJump.FixedCost, path.Jumps[0].FixedCost);
        Assert.Equal(secondJump.FixedCost, path.Jumps[1].FixedCost);
        Assert.Equal(3, path.SearchCost, 10);
        Assert.Equal(path.SearchCost, path.TotalCost, 10);
    }
}

public sealed class NavMeshQueryOptionsTests
{
    [Fact]
    public void FindPath_WhenUsingExplicitHeuristicWeight_ShouldExposeCapturedWeight()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 1), new NavMeshPoint(0, 1)],
            [new NavMeshConvexPolygon([0, 1, 2, 3])]);
        NavMeshQueryOptions options = new NavMeshQueryOptions { HeuristicWeight = 1.5 };

        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(0.25, 0.5), new NavMeshPoint(1.75, 0.5),
            navMesh.CreateQueryWorkspace(), NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, options);

        Assert.NotNull(path);
        Assert.Equal(1.5, path.HeuristicWeight);
        Assert.False(path.IsSearchOptimal);
        Assert.Equal(0, path.SearchCost);
    }

    [Fact]
    public void HeuristicWeight_WhenSetBelowOne_ShouldThrow()
    {
        NavMeshQueryOptions options = new NavMeshQueryOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.HeuristicWeight = 0.99);
    }
}

public sealed class NavMeshNearestPointTests
{
    [Fact]
    public void TryFindNearestPoint_WhenPointIsOutsideMesh_ShouldProjectToBoundary()
    {
        Mesh navMesh = Mesh.Create([new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(0, 2)],
            [new NavMeshTriangle(0, 1, 2)]);

        bool found = navMesh.TryFindNearestPoint(new NavMeshPoint(2, 2), out int triangleIndex,
            out NavMeshPoint nearestPoint);

        Assert.True(found);
        Assert.Equal(0, triangleIndex);
        Assert.Equal(new NavMeshPoint(1, 1), nearestPoint);
    }

    [Fact]
    public void TryFindNearestPoint_WhenFilterExcludesCloserTriangle_ShouldUseAllowedTriangle()
    {
        Mesh navMesh = Mesh.Create(
            [
                new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1), new NavMeshPoint(3, 0),
                new NavMeshPoint(4, 0), new NavMeshPoint(3, 1)
            ],
            [new NavMeshTriangle(0, 1, 2, 0, 0b_0001), new NavMeshTriangle(3, 4, 5, 0, 0b_0010)]);

        bool found = navMesh.TryFindNearestPoint(new NavMeshPoint(0.1, 0.1),
            new NavMeshQueryFilter { IncludedFlags = 0b_0010 }, out int triangleIndex, out _);

        Assert.True(found);
        Assert.Equal(1, triangleIndex);
    }

    [Fact]
    public void TryFindNearestLocation_WhenFilteredConvexPolygonIsNearestAllowed_ShouldReturnCachedLocation()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1)
        ],
        [
            new NavMeshConvexPolygon([0, 1, 2, 3], 0, 0b_0001),
            new NavMeshConvexPolygon([4, 5, 6, 7], 0, 0b_0010)
        ]);
        NavMeshQueryFilter filter = new NavMeshQueryFilter { IncludedFlags = 0b_0010 };

        bool found = navMesh.TryFindNearestLocation(new NavMeshPoint(0.25, 0.5), filter,
            out NavMeshLocation location);

        Assert.True(found);
        Assert.True(navMesh.IsValidPolygonRef(location.PolygonRef));
        Assert.Equal(1, location.PolygonRef.Index);
        Assert.Equal(new NavMeshPoint(3, 0.5), location.Position);
    }

    [Fact]
    public void TryFindNearestLocation_WhenUsingHalfExtents_ShouldIgnorePolygonsOutsideSearchBounds()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1)
        ],
        [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([4, 5, 6, 7])]);

        bool found = navMesh.TryFindNearestLocation(new NavMeshPoint(2.7, 0.5), new NavMeshPoint(0.5, 0.5),
            NavMeshQueryDefaults.Filter, out NavMeshLocation location);

        Assert.True(found);
        Assert.Equal(1, location.PolygonRef.Index);
        Assert.Equal(new NavMeshPoint(3, 0.5), location.Position);
    }

    [Fact]
    public void TryFindRandomPoint_WhenFilterAllowsOneTriangle_ShouldSelectThatTriangle()
    {
        Mesh navMesh = Mesh.Create(
            [
                new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1), new NavMeshPoint(3, 0),
                new NavMeshPoint(4, 0), new NavMeshPoint(3, 1)
            ],
            [new NavMeshTriangle(0, 1, 2, 0, 0b_0001), new NavMeshTriangle(3, 4, 5, 0, 0b_0010)]);

        bool found = navMesh.TryFindRandomPoint(new Random(1), new NavMeshQueryFilter { IncludedFlags = 0b_0010 },
            out int triangleIndex, out NavMeshPoint point);

        Assert.True(found);
        Assert.Equal(1, triangleIndex);
        Assert.True(point.X >= 3 && point.Y >= 0 && point.X + point.Y <= 4);
    }

    [Fact]
    public void TryFindRandomPoint_WhenUsingKnownPolygon_ShouldReturnLocationInsideThatPolygon()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)],
            [new NavMeshConvexPolygon([0, 1, 2, 3])]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(1, 1), out NavMeshLocation source));

        bool found = navMesh.TryFindRandomPoint(source.PolygonRef, new Random(123), out NavMeshLocation location);

        Assert.True(found);
        Assert.Equal(source.PolygonRef, location.PolygonRef);
        Assert.True(navMesh.TryFindLocation(location.Position, out NavMeshLocation resolved));
        Assert.Equal(source.PolygonRef, resolved.PolygonRef);
    }
}

public sealed class NavMeshBoundaryTests
{
    [Fact]
    public void CopyTrianglesOverlappingBounds_WhenBoundsCoverOneDisconnectedTriangle_ShouldReturnOnlyThatTriangle()
    {
        Mesh navMesh = Mesh.Create(
            [
                new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(0, 1), new NavMeshPoint(3, 0),
                new NavMeshPoint(4, 0), new NavMeshPoint(3, 1)
            ],
            [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(3, 4, 5)]);
        Span<int> triangles = stackalloc int[2];

        int count = navMesh.CopyTrianglesOverlappingBounds(new NavMeshBounds(2.5, -0.5, 4.5, 1.5), triangles);

        Assert.Equal(1, count);
        Assert.Equal(1, triangles[0]);
    }

    [Fact]
    public void CopyPolygonsOverlappingBounds_WhenFilterAllowsOnePolygon_ShouldReturnBoundReference()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1)
        ],
        [
            new NavMeshConvexPolygon([0, 1, 2, 3], 0, 0b_0001),
            new NavMeshConvexPolygon([4, 5, 6, 7], 0, 0b_0010)
        ]);
        Span<NavMeshPolygonRef> polygons = stackalloc NavMeshPolygonRef[2];

        int count = navMesh.CopyPolygonsOverlappingBounds(new NavMeshBounds(-1, -1, 5, 2),
            new NavMeshQueryFilter { IncludedFlags = 0b_0010 }, polygons);

        Assert.Equal(1, count);
        Assert.Equal(1, polygons[0].Index);
        Assert.True(navMesh.IsValidPolygonRef(polygons[0]));
    }

    [Fact]
    public void CopyBoundarySegments_WhenTwoTrianglesShareOneEdge_ShouldReturnFourOuterEdges()
    {
        Mesh navMesh =
            Mesh.Create(
                [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)],
                [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
        Span<NavMeshSegment> segments = stackalloc NavMeshSegment[4];

        int count = navMesh.CopyBoundarySegments(segments);

        Assert.Equal(4, count);
    }

    [Fact]
    public void CopyPolygonWallSegments_WhenNeighborIsFilteredOut_ShouldExposeSharedEdgeAsWall()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(2, 0), new NavMeshPoint(2, 1)
        ],
        [
            new NavMeshConvexPolygon([0, 1, 2, 3], 0, 0b_0001),
            new NavMeshConvexPolygon([1, 4, 5, 2], 0, 0b_0010)
        ]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation location));
        Span<NavMeshSegment> walls = stackalloc NavMeshSegment[4];

        int count = navMesh.CopyPolygonWallSegments(location.PolygonRef,
            new NavMeshQueryFilter { IncludedFlags = 0b_0001 }, walls);

        Assert.Equal(4, count);
        Assert.Contains(new NavMeshSegment(new NavMeshPoint(1, 0), new NavMeshPoint(1, 1)), walls.ToArray());
    }
}

public sealed class NavMeshWallTests
{
    [Fact]
    public void TryFindDistanceToWall_WhenPointIsInsideSquare_ShouldReturnNearestWall()
    {
        Mesh navMesh =
            Mesh.Create(
                [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)],
                [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);

        bool found = navMesh.TryFindDistanceToWall(new NavMeshPoint(0.5, 1), 2, out NavMeshWallHit hit);

        Assert.True(found);
        Assert.Equal(0.5, hit.Distance, 10);
        Assert.Equal(new NavMeshPoint(0, 1), hit.Position);
    }
}

public sealed class NavMeshRaycastTests
{
    [Fact]
    public void Raycast_WhenSegmentCrossesPortal_ShouldReturnTraversedCorridor()
    {
        Mesh navMesh = CreateSquare();

        NavMeshRaycastResult result = navMesh.Raycast(new NavMeshPoint(0.5, 0.2), new NavMeshPoint(0.2, 1.5));

        Assert.True(result.ReachedEnd);
        Assert.Null(result.Hit);
        Assert.Equal([0, 1], result.Corridor);
    }

    [Fact]
    public void Raycast_WhenFilterRejectsNextTriangle_ShouldStopAtPortal()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)],
            [new NavMeshTriangle(0, 1, 2, 0, 0b_0001), new NavMeshTriangle(0, 2, 3, 0, 0b_0010)]);

        NavMeshRaycastResult result = navMesh.Raycast(new NavMeshPoint(0.5, 0.2), new NavMeshPoint(0.2, 1.5),
            new NavMeshQueryFilter { IncludedFlags = 0b_0001 });

        Assert.False(result.ReachedEnd);
        Assert.NotNull(result.Hit);
        Assert.Equal(0.1875, result.Hit.Value.T, 10);
        Assert.Equal([0], result.Corridor);
    }

    [Fact]
    public void TryRaycastBoundary_WhenSegmentLeavesSquare_ShouldReturnFirstHit()
    {
        Mesh navMesh = CreateSquare();

        bool hit = navMesh.TryRaycastBoundary(new NavMeshPoint(1, 1), new NavMeshPoint(3, 1),
            out NavMeshRaycastHit result);

        Assert.True(hit);
        Assert.Equal(0.5, result.T, 10);
        Assert.Equal(new NavMeshPoint(2, 1), result.Position);
        Assert.Equal(new NavMeshPoint(1, 0), result.Normal);
    }

    [Fact]
    public void TryMoveAlongSurface_WhenSegmentLeavesSquare_ShouldStopAtBoundary()
    {
        Mesh navMesh = CreateSquare();

        bool reachedEnd =
            navMesh.TryMoveAlongSurface(new NavMeshPoint(1, 1), new NavMeshPoint(3, 1), out NavMeshPoint position);

        Assert.False(reachedEnd);
        Assert.Equal(new NavMeshPoint(2, 1), position);
    }

    [Fact]
    public void TryMoveAlongSurface_WhenSegmentStaysInSquare_ShouldReachEnd()
    {
        Mesh navMesh = CreateSquare();

        bool reachedEnd = navMesh.TryMoveAlongSurface(new NavMeshPoint(0.5, 0.5), new NavMeshPoint(1.5, 1.5),
            out NavMeshPoint position);

        Assert.True(reachedEnd);
        Assert.Equal(new NavMeshPoint(1.5, 1.5), position);
    }

    private static Mesh CreateSquare()
    {
        return Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)],
            [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
    }
}

public sealed class NavMeshLocalQueryTests
{
    [Fact]
    public void TryFindPolygonsAroundCircle_WhenOnlyFirstPortalTouchesCircle_ShouldReturnReachablePrefix()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(2, 0), new NavMeshPoint(2, 1), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1)
        ],
        [
            new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([1, 4, 5, 2]),
            new NavMeshConvexPolygon([4, 6, 7, 5])
        ]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation start));
        Span<NavMeshLocalPolygon> result = stackalloc NavMeshLocalPolygon[2];

        bool complete = navMesh.TryFindPolygonsAroundCircle(start, 0.75, navMesh.CreateQueryWorkspace(),
            NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, result, out int resultCount);

        Assert.True(complete);
        Assert.Equal(2, resultCount);
        Assert.Equal(0, result[0].PolygonRef.Index);
        Assert.Equal(NavMeshPolygonRef.Invalid, result[0].ParentPolygonRef);
        Assert.Equal(1, result[1].PolygonRef.Index);
        Assert.Equal(result[0].PolygonRef, result[1].ParentPolygonRef);
        Assert.Equal(0.5, result[1].Cost, 10);
    }

    [Fact]
    public void TryFindPolygonsAroundCircle_WhenDestinationIsTooSmall_ShouldReturnRequiredCount()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(2, 0), new NavMeshPoint(2, 1)
        ],
        [new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([1, 4, 5, 2])]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation start));
        Span<NavMeshLocalPolygon> result = stackalloc NavMeshLocalPolygon[1];

        bool complete = navMesh.TryFindPolygonsAroundCircle(start, 0.75, navMesh.CreateQueryWorkspace(),
            NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, result, out int resultCount);

        Assert.False(complete);
        Assert.Equal(2, resultCount);
        Assert.Equal(0, result[0].PolygonRef.Index);
    }

    [Fact]
    public void TryFindRandomPointAroundCircle_ShouldOnlySelectPolygonsReachableWithinCircle()
    {
        Mesh navMesh = Mesh.Create(
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(2, 0), new NavMeshPoint(2, 1), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1)
        ],
        [
            new NavMeshConvexPolygon([0, 1, 2, 3]), new NavMeshConvexPolygon([1, 4, 5, 2]),
            new NavMeshConvexPolygon([4, 6, 7, 5])
        ]);
        Assert.True(navMesh.TryFindLocation(new NavMeshPoint(0.5, 0.5), out NavMeshLocation start));

        bool found = navMesh.TryFindRandomPointAroundCircle(start, 0.75, new Random(1),
            navMesh.CreateQueryWorkspace(), NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy,
            out NavMeshLocation location);

        Assert.True(found);
        Assert.InRange(location.PolygonRef.Index, 0, 1);
        Assert.True(navMesh.TryFindLocation(location.Position, out NavMeshLocation resolved));
        Assert.Equal(location.PolygonRef, resolved.PolygonRef);
    }
}

public sealed class NavMeshTraversalCostTests
{
    [Fact]
    public void FindPath_WhenDirectCorridorHasHigherAreaCost_ShouldUseLongerCorridor()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(4, 0), new NavMeshPoint(6, 0),
            new NavMeshPoint(0, 1), new NavMeshPoint(2, 1), new NavMeshPoint(4, 1), new NavMeshPoint(6, 1),
            new NavMeshPoint(0, 3), new NavMeshPoint(2, 3), new NavMeshPoint(4, 3), new NavMeshPoint(6, 3),
            new NavMeshPoint(0, 4), new NavMeshPoint(2, 4), new NavMeshPoint(4, 4), new NavMeshPoint(6, 4)
        ];
        NavMeshTriangle[] triangles =
        [
            new NavMeshTriangle(0, 1, 5, 9), new NavMeshTriangle(0, 5, 4, 9), new NavMeshTriangle(1, 2, 6, 9),
            new NavMeshTriangle(1, 6, 5, 9), new NavMeshTriangle(2, 3, 7, 9), new NavMeshTriangle(2, 7, 6, 9),
            new NavMeshTriangle(8, 9, 13), new NavMeshTriangle(8, 13, 12), new NavMeshTriangle(9, 10, 14),
            new NavMeshTriangle(9, 14, 13), new NavMeshTriangle(10, 11, 15), new NavMeshTriangle(10, 15, 14),
            new NavMeshTriangle(4, 5, 9), new NavMeshTriangle(4, 9, 8), new NavMeshTriangle(6, 7, 11),
            new NavMeshTriangle(6, 11, 10)
        ];
        Mesh navMesh = Mesh.Create(vertices, triangles);
        NavMeshPath? path = navMesh.FindPath(new NavMeshPoint(1, 0.5), new NavMeshPoint(5, 0.5),
            navMesh.CreateQueryWorkspace(), NavMeshQueryDefaults.Filter, new AreaCostPolicy());

        Assert.NotNull(path);
        Assert.Contains(6, path.Corridor);
    }

    private sealed class AreaCostPolicy : INavMeshTraversalCostPolicy
    {
        public double MinimumMultiplier => 1;
        public double GetMultiplier(int fromAreaId, int toAreaId) => toAreaId == 9 ? 100 : 1;
    }
}

public sealed class NavMeshBinaryTests
{
    [Fact]
    public void WriteThenRead_WhenMeshContainsAreasAndFlags_ShouldPreserveQueryData()
    {
        Mesh original = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 2), new NavMeshPoint(0, 2)],
            [new NavMeshTriangle(0, 1, 2, 3, 0b_0011), new NavMeshTriangle(0, 2, 3, 5, 0b_0101)]);
        using MemoryStream stream = new MemoryStream();

        NavMeshBinary.Write(stream, original);
        stream.Position = 0;
        Mesh restored = NavMeshBinary.Read(stream);

        Assert.Equal(original.VertexCount, restored.VertexCount);
        Assert.Equal(original.TriangleCount, restored.TriangleCount);
        Assert.Equal(3, restored.GetAreaId(0));
        Assert.Equal(0b_0101u, restored.GetFlags(1));
        Assert.NotNull(restored.FindPath(new NavMeshPoint(1.8, 0.2), new NavMeshPoint(0.2, 1.8)));
    }

    [Fact]
    public void WriteThenRead_WhenMeshContainsConvexPolygonsAndJump_ShouldPreserveTopology()
    {
        NavMeshPoint[] vertices =
        [
            new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1),
            new NavMeshPoint(3, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 1), new NavMeshPoint(3, 1)
        ];
        NavMeshJumpConnection jump = new NavMeshJumpConnection(new NavMeshPoint(0.75, 0.1),
            new NavMeshPoint(3.25, 0.1), 2, true);
        Mesh original = Mesh.Create(vertices,
            [new NavMeshConvexPolygon([0, 1, 2, 3], 4), new NavMeshConvexPolygon([4, 5, 6, 7], 9)], [jump]);
        using MemoryStream stream = new MemoryStream();

        NavMeshBinary.Write(stream, original);
        stream.Position = 0;
        Mesh restored = NavMeshBinary.Read(stream);

        Assert.Equal(2, restored.PolygonCount);
        Assert.Equal(1, restored.JumpConnectionCount);
        NavMeshPath? path = restored.FindPath(new NavMeshPoint(0.5, 0.1), new NavMeshPoint(3.5, 0.1));
        Assert.NotNull(path);
        Assert.Single(path.Jumps);
        Assert.Equal(2, path.Jumps[0].FixedCost);
    }
}

public sealed class TiledNavMeshTests
{
    [Fact]
    public void ApplyUpdates_WhenTilesShareEdge_ShouldCreateOneCrossTileSnapshot()
    {
        TiledNavMesh tiledNavMesh = new TiledNavMesh();
        Mesh first = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(1, 0), new NavMeshPoint(1, 1), new NavMeshPoint(0, 1)],
            [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
        Mesh second = Mesh.Create(
            [new NavMeshPoint(1, 0), new NavMeshPoint(2, 0), new NavMeshPoint(2, 1), new NavMeshPoint(1, 1)],
            [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);

        bool changed = tiledNavMesh.ApplyUpdates(
        [
            new NavMeshTileUpdate(new NavMeshTileId(0, 0), first),
            new NavMeshTileUpdate(new NavMeshTileId(1, 0), second)
        ]);

        Mesh? snapshot = tiledNavMesh.Snapshot;

        Assert.True(changed);
        Assert.Equal(2, tiledNavMesh.TileCount);
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.FindPath(new NavMeshPoint(0.2, 0.2), new NavMeshPoint(1.8, 0.8)));
        Assert.False(tiledNavMesh.ApplyUpdates([new NavMeshTileUpdate(new NavMeshTileId(0, 0), first)]));
        Assert.Same(snapshot, tiledNavMesh.Snapshot);
        Assert.True(tiledNavMesh.RemoveTile(new NavMeshTileId(1, 0)));
        Assert.Null(tiledNavMesh.Snapshot?.FindPath(new NavMeshPoint(0.2, 0.2), new NavMeshPoint(1.8, 0.8)));
    }
}

public sealed class DynamicNavMeshTests
{
    [Fact]
    public void AddAndRemoveObstacle_ShouldPublishUpdatedSnapshots()
    {
        DynamicNavMesh dynamicNavMesh = new DynamicNavMesh(new NavMeshBuildInput(new NavMeshPolygon([
            new NavMeshPoint(0, 0), new NavMeshPoint(4, 0), new NavMeshPoint(4, 4), new NavMeshPoint(0, 4)
        ])));
        NavMeshPoint start = new NavMeshPoint(0.5, 1.5);
        NavMeshPoint goal = new NavMeshPoint(3.5, 1.5);

        int obstacleId = dynamicNavMesh.AddObstacle(new NavMeshPolygon([
            new NavMeshPoint(1, 1), new NavMeshPoint(2, 1), new NavMeshPoint(2, 2), new NavMeshPoint(1, 2)
        ]));
        NavMeshPath? withObstacle = dynamicNavMesh.Snapshot.FindPath(start, goal);

        Assert.Equal(1, dynamicNavMesh.ObstacleCount);
        Assert.NotNull(withObstacle);
        Assert.True(withObstacle.Points.Count > 2);
        Assert.True(dynamicNavMesh.RemoveObstacle(obstacleId));
        Assert.Equal(0, dynamicNavMesh.ObstacleCount);
        Assert.Equal([start, goal], dynamicNavMesh.Snapshot.FindPath(start, goal)?.Points);
    }
}

public sealed class NavMeshCrowdTests
{
    [Fact]
    public void Update_WhenAgentHasPath_ShouldAdvanceAndReachTarget()
    {
        Mesh navMesh = Mesh.Create(
            [new NavMeshPoint(0, 0), new NavMeshPoint(3, 0), new NavMeshPoint(3, 1), new NavMeshPoint(0, 1)],
            [new NavMeshTriangle(0, 1, 2), new NavMeshTriangle(0, 2, 3)]);
        NavMeshCrowd crowd = new NavMeshCrowd(navMesh);
        int agentId = crowd.AddAgent(new NavMeshPoint(0.25, 0.25), 1);

        Assert.True(crowd.RequestMoveTarget(agentId, new NavMeshPoint(2.25, 0.25)));
        crowd.Update(1);
        Assert.True(crowd.TryGetAgentState(agentId, out NavMeshCrowdAgentState midway));
        crowd.Update(1);
        Assert.True(crowd.TryGetAgentState(agentId, out NavMeshCrowdAgentState finished));

        Assert.Equal(new NavMeshPoint(1.25, 0.25), midway.Position);
        Assert.Equal(new NavMeshPoint(2.25, 0.25), finished.Position);
        Assert.False(finished.HasPath);
    }
}

/// <summary>
/// 验证与 HexCube 寻路对比使用的地图可被 NavMesh 成功查询.
/// </summary>
public sealed class NavMeshPathfindingComparisonMapTests
{
    [Fact]
    /// <summary>
    /// 验证同一张六边形地图在 Hex A* 与 NavMesh A* 中均存在可达路径.
    /// </summary>
    public void RunNavMeshAStar_WhenUsingSameHexComparisonMap_ShouldFindPath()
    {
        Assert.True(NavMeshPathfindingComparisonRunTest.RunHexAStar() > 0);
        Assert.True(NavMeshPathfindingComparisonRunTest.RunNavMeshAStar() > 0);
    }
}
