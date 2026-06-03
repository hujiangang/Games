using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "NavMeshSettings", menuName = "导航/NavMesh Settings")]
public class NavMeshSettings : ScriptableObject
{
    [Header("Unity NavMesh 烘焙参数")]
    public float AgentRadius = 0.3f;
    public float AgentHeight = 2f;
    public float StepHeight = 0.4f;
    public float MaxSlope = 35f;

    [Header("导出源过滤")]
    public LayerMask WalkableLayers = 1 << 0;

    [Header("DotRecast 导出参数")]
    public float DotRecastCellSize = 0.16666667f;
    public float DotRecastCellHeight = 0.1f;
    public int DotRecastMaxVertsPerPoly = 6;
    public bool DotRecastBuildDetailMesh = true;

    [Header("导出文件")]
    public string ExportFileName = "SceneNavMesh.navdata";

    public string GetResourceAssetName()
    {
        string fileName = string.IsNullOrWhiteSpace(ExportFileName) ? "SceneNavMesh" : ExportFileName;
        return Path.GetFileNameWithoutExtension(fileName);
    }

    public string GetRuntimeDataAssetName()
    {
        return GetResourceAssetName();
    }

    public string GetRuntimeDataResourcePath()
    {
        return $"NavMesh/{GetRuntimeDataAssetName()}";
    }

    public string GetRuntimeDataFileName()
    {
        return $"{GetRuntimeDataAssetName()}.bytes";
    }
}
