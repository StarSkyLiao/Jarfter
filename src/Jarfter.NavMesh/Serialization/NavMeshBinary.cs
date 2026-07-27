using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Topology;
using System.Text;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Serialization;

/// <summary>
/// 读写 Jarfter.NavMesh 的稳定二进制静态网格格式.
/// 格式保存顶点、凸多边形和跳跃连接数据, 相邻关系与 BVH 会在读取时重新构建.
/// </summary>
public static class NavMeshBinary
{
    private const int Magic = 0x4A4E4D32;
    private const int Version = 2;
    private const int MaxElementCount = 16 * 1024 * 1024;

    /// <summary>
    /// 将导航网格写入目标流, 不关闭目标流.
    /// </summary>
    /// <param name="destination">可写入的目标流.</param>
    /// <param name="navMesh">待序列化的不可变导航网格.</param>
    public static void Write(Stream destination, Mesh navMesh)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(navMesh);
        if (!destination.CanWrite) throw new ArgumentException("目标流必须可写入.", nameof(destination));

        NavMeshPoint[] vertices = new NavMeshPoint[navMesh.VertexCount];
        NavMeshConvexPolygon[] polygons = new NavMeshConvexPolygon[navMesh.PolygonCount];
        NavMeshJumpConnection[] jumpConnections = new NavMeshJumpConnection[navMesh.JumpConnectionCount];
        navMesh.CopyVertices(vertices);
        navMesh.CopyPolygons(polygons);
        navMesh.CopyJumpConnections(jumpConnections);
        using BinaryWriter writer = new BinaryWriter(destination, Encoding.UTF8, true);
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(vertices.Length);
        writer.Write(polygons.Length);
        writer.Write(jumpConnections.Length);
        foreach (NavMeshPoint vertex in vertices)
        {
            writer.Write(vertex.X);
            writer.Write(vertex.Y);
        }

        foreach (NavMeshConvexPolygon polygon in polygons)
        {
            writer.Write(polygon.VertexIndices.Count);
            foreach (int vertexIndex in polygon.VertexIndices) writer.Write(vertexIndex);
            writer.Write(polygon.AreaId);
            writer.Write(polygon.Flags);
        }

        foreach (NavMeshJumpConnection jumpConnection in jumpConnections)
        {
            writer.Write(jumpConnection.Start.X);
            writer.Write(jumpConnection.Start.Y);
            writer.Write(jumpConnection.End.X);
            writer.Write(jumpConnection.End.Y);
            writer.Write(jumpConnection.FixedCost);
            writer.Write(jumpConnection.IsBidirectional);
        }
    }

    /// <summary>
    /// 从源流读取导航网格, 不关闭源流.
    /// </summary>
    /// <param name="source">可读取的二进制格式源流.</param>
    /// <returns>重新建立相邻关系与 BVH 的不可变导航网格.</returns>
    /// <exception cref="InvalidDataException">格式头、版本或元素数量无效.</exception>
    public static Mesh Read(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("源流必须可读取.", nameof(source));

        using BinaryReader reader = new BinaryReader(source, Encoding.UTF8, true);
        if (reader.ReadInt32() != Magic) throw new InvalidDataException("不是 Jarfter.NavMesh 二进制格式.");
        int version = reader.ReadInt32();
        if (version is not 1 and not Version) throw new InvalidDataException("不支持的 Jarfter.NavMesh 二进制格式版本.");
        int vertexCount = reader.ReadInt32();
        int polygonOrTriangleCount = reader.ReadInt32();
        if (vertexCount is <= 0 or > MaxElementCount || polygonOrTriangleCount is <= 0 or > MaxElementCount)
            throw new InvalidDataException("导航网格元素数量无效或超过安全限制.");
        int jumpConnectionCount = version == 1 ? 0 : reader.ReadInt32();
        if (jumpConnectionCount is < 0 or > MaxElementCount)
            throw new InvalidDataException("跳跃连接数量无效或超过安全限制.");

        NavMeshPoint[] vertices = new NavMeshPoint[vertexCount];
        for (int index = 0; index < vertices.Length; index++)
            vertices[index] = new NavMeshPoint(reader.ReadDouble(), reader.ReadDouble());
        if (version == 1)
        {
            NavMeshTriangle[] triangles = new NavMeshTriangle[polygonOrTriangleCount];
            for (int index = 0; index < triangles.Length; index++)
                triangles[index] = new NavMeshTriangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                    reader.ReadInt32(), reader.ReadUInt32());
            return Mesh.Create(vertices, triangles);
        }

        NavMeshConvexPolygon[] polygons = new NavMeshConvexPolygon[polygonOrTriangleCount];
        for (int index = 0; index < polygons.Length; index++)
        {
            int polygonVertexCount = reader.ReadInt32();
            if (polygonVertexCount is < 3 or > MaxElementCount)
                throw new InvalidDataException("凸多边形顶点数量无效或超过安全限制.");
            int[] polygonVertices = new int[polygonVertexCount];
            for (int vertexIndex = 0; vertexIndex < polygonVertices.Length; vertexIndex++)
                polygonVertices[vertexIndex] = reader.ReadInt32();
            polygons[index] = new NavMeshConvexPolygon(polygonVertices, reader.ReadInt32(), reader.ReadUInt32());
        }

        NavMeshJumpConnection[] jumpConnections = new NavMeshJumpConnection[jumpConnectionCount];
        for (int index = 0; index < jumpConnections.Length; index++)
        {
            NavMeshPoint start = new NavMeshPoint(reader.ReadDouble(), reader.ReadDouble());
            NavMeshPoint end = new NavMeshPoint(reader.ReadDouble(), reader.ReadDouble());
            jumpConnections[index] = new NavMeshJumpConnection(start, end, reader.ReadDouble(), reader.ReadBoolean());
        }

        return Mesh.Create(vertices, polygons, jumpConnections);
    }
}
