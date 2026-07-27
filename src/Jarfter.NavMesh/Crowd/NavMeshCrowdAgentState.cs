using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Crowd;

/// <summary>
/// 描述 Crowd 中一个 agent 的当前可观察状态.
/// </summary>
public readonly record struct NavMeshCrowdAgentState(
    int Id,
    NavMeshPoint Position,
    NavMeshPoint? Target,
    bool HasPath
);
