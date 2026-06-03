using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "NavMeshSettings", menuName = "导航/NavMesh Settings")]
public class NavMeshSettings:ScriptableObject 
{
    // 烘焙参数（和你河道Y=-0.4完全对应）
    public float AgentRadius = 0.3f;
    public float AgentHeight = 2f;
    public float StepHeight = 0.4f; // 刚好匹配河道与地面高差
    public float MaxSlope = 35f;

    // 层过滤（只烘焙Walkable层：地面+桥，河道Cube不烘焙）
    public LayerMask WalkableLayers = 1 << 0; // 假设Walkable在Layer0

    // 导出的数据文件名
    public string ExportFileName = "SceneNavMesh.navdata";

    public string GetResourceAssetName()
    {
        string fileName = string.IsNullOrWhiteSpace(ExportFileName) ? "SceneNavMesh" : ExportFileName;
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
