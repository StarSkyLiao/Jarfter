# Jarfter.NavMesh

面向 `.NET 10+` 的纯二维、全 `double` 导航网格库。它借鉴 Detour 的查询语义，但不包含 Recast 的体素化或任何 3D 功能。

## 当前能力

- 从简单边界和非重叠障碍物构建三角导航网格。
- 可直接由逆时针凸多边形创建网格, 并支持单向或双向跳跃连接与固定跳跃开销。
- Area、Flags、过滤器与区域移动代价。
- A* 路径、funnel 航点、最近点、随机点、墙距、边界与 corridor raycast。
- 凸环 `AgentRadius` 安全边距烘焙。
- 二进制读写、跨共享边 tile 快照、低频动态障碍物重建。
- 基础 Crowd 路径推进与网格快照切换。

## 基本用法

```csharp
using Jarfter.NavMesh.Build;
using Jarfter.NavMesh.Geometry;

NavMeshBuildInput input = new NavMeshBuildInput(new NavMeshPolygon([
    new NavMeshPoint(0, 0),
    new NavMeshPoint(10, 0),
    new NavMeshPoint(10, 10),
    new NavMeshPoint(0, 10)
]));

var navMesh = NavMeshBuilder.Build(input, new NavMeshBuildOptions
{
    AgentRadius = 0.5,
    AreaId = 1,
    Flags = 0b_0001
});

var path = navMesh.FindPath(new NavMeshPoint(1, 1), new NavMeshPoint(9, 9));
```

## 边界

- `AgentRadius` 当前要求外边界和障碍物环均为严格凸环；凹环会明确拒绝。
- Crowd 当前负责路径推进，不包含 agent 间局部避障。
- Tile 通过完全相等的双精度边界顶点连接；相邻 tile 必须共享同一组边界坐标。
- 凸多边形直接作为 A* 查询节点；几何细节查询保留内部三角化结果。
