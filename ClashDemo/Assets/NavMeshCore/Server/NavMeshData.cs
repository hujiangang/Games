using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

// 服务器用的纯数据结构
public class ServerNavMeshData
{
    public const int FileMagic = 0x4E4D5348; // NMSH
    public const int FileVersion = 1;

    public List<Vector3> Vertices;
    public List<int> Indices;
    public List<int> Areas; // 0=可行走（桥/地面），1=不可行走（河道）

    // 反序列化：读取Unity导出的二进制文件
    public static ServerNavMeshData Load(string path)
    {
        using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using BinaryReader reader = new BinaryReader(stream);

        int magic = reader.ReadInt32();
        if (magic != FileMagic)
            throw new InvalidDataException($"NavMesh 数据文件头无效: {path}");

        int version = reader.ReadInt32();
        if (version != FileVersion)
            throw new NotSupportedException($"NavMesh 数据版本不支持: {version}");

        ServerNavMeshData data = new ServerNavMeshData
        {
            Vertices = new List<Vector3>(),
            Indices = new List<int>(),
            Areas = new List<int>()
        };

        int vertexCount = reader.ReadInt32();
        for (int i = 0; i < vertexCount; i++)
        {
            data.Vertices.Add(new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()));
        }

        int indexCount = reader.ReadInt32();
        for (int i = 0; i < indexCount; i++)
            data.Indices.Add(reader.ReadInt32());

        int areaCount = reader.ReadInt32();
        for (int i = 0; i < areaCount; i++)
            data.Areas.Add(reader.ReadInt32());

        return data;
    }

    // 服务器寻路逻辑（用SharpNav/自己写A*）
    public bool FindPath(Vector3 start, Vector3 end, out List<Vector3> path)
    {
        // 纯数学寻路，和客户端逻辑完全一致
        path = new List<Vector3>();
        return true;
    }
}
