using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBakeWindow : EditorWindow
{
    private static NavMeshDataInstance s_previewInstance;
    private NavMeshSettings _settings;

    public static void ShowWindow(NavMeshSettings settings)
    {
        var window = GetWindow<NavMeshBakeWindow>("NavMesh 全局烘焙");
        window._settings = settings;
        window.Show();
    }

    void OnGUI()
    {
        if (_settings == null)
        {
            EditorGUILayout.HelpBox("请先赋值 NavMeshSettings", MessageType.Error);
            return;
        }

        SerializedObject so = new SerializedObject(_settings);
        SerializedProperty prop = so.GetIterator();

        while (prop.NextVisible(true))
            EditorGUILayout.PropertyField(prop);

        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("一键烘焙 NavMesh", GUILayout.Height(40)))
            BakeNavMesh();

        if (GUILayout.Button("导出服务器 NavMesh 数据", GUILayout.Height(30)))
            ExportForServer();
    }

    private void BakeNavMesh()
    {
        NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);

        buildSettings.agentRadius = _settings.AgentRadius;
        buildSettings.agentHeight = _settings.AgentHeight;
        buildSettings.agentClimb = _settings.StepHeight;
        buildSettings.agentSlope = _settings.MaxSlope;

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            root: null,
            includedLayerMask: _settings.WalkableLayers,
            geometry: NavMeshCollectGeometry.RenderMeshes,
            defaultArea: 0,
            markups: new List<NavMeshBuildMarkup>(),
            results: sources);

        if (sources.Count == 0)
        {
            Debug.LogWarning("NavMesh 烘焙取消：在当前打开的场景里没有找到可用于烘焙的 Walkable 几何。");
            return;
        }

        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            buildSettings,
            sources,
            new Bounds(),
            Vector3.zero,
            Quaternion.identity);

        if (navMeshData == null)
        {
            Debug.LogError("NavMesh 烘焙失败：BuildNavMeshData 返回了空结果。");
            return;
        }

        string assetPath = GetRuntimeAssetPath();
        NavMeshData existingData = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
        NavMeshData savedData;

        if (existingData == null)
        {
            AssetDatabase.CreateAsset(navMeshData, assetPath);
            savedData = navMeshData;
        }
        else
        {
            EditorUtility.CopySerialized(navMeshData, existingData);
            DestroyImmediate(navMeshData);
            savedData = existingData;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RegisterPreviewNavMesh(savedData);

        Debug.Log($"✅ 烘焙完成，NavMeshData 已保存到 {assetPath}");
    }

    private void ExportForServer()
    {
        NavMeshData navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(GetRuntimeAssetPath());
        if (navMeshData == null)
        {
            Debug.LogError("导出失败：找不到已烘焙的 NavMeshData 资源，请先执行一次烘焙。");
            return;
        }

        RegisterPreviewNavMesh(navMeshData);

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length == 0 || tri.indices == null || tri.indices.Length == 0)
        {
            Debug.LogError("导出失败：当前 NavMesh 三角化结果为空，请先确认烘焙成功并且场景中存在可行走区域。");
            return;
        }

        string path = $"Assets/NavMeshCore/Server/{_settings.ExportFileName}";
        EnsureFolderExists("Assets/NavMeshCore/Server");

        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(ServerNavMeshData.FileMagic);
            writer.Write(ServerNavMeshData.FileVersion);

            writer.Write(tri.vertices.Length);
            foreach (Vector3 vertex in tri.vertices)
            {
                writer.Write(vertex.x);
                writer.Write(vertex.y);
                writer.Write(vertex.z);
            }

            writer.Write(tri.indices.Length);
            foreach (int index in tri.indices)
                writer.Write(index);

            int[] areas = tri.areas ?? new int[0];
            writer.Write(areas.Length);
            foreach (int area in areas)
                writer.Write(area);
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ 导出完成：{path}，顶点 {tri.vertices.Length}，三角形 {tri.indices.Length / 3}");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] segments = folderPath.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);

            current = next;
        }
    }

    private string GetRuntimeAssetPath()
    {
        string assetDirectory = "Assets/Resources";
        EnsureFolderExists(assetDirectory);
        return $"{assetDirectory}/{_settings.GetResourceAssetName()}.asset";
    }

    private static void RegisterPreviewNavMesh(NavMeshData navMeshData)
    {
        if (s_previewInstance.valid)
            s_previewInstance.Remove();

        s_previewInstance = NavMesh.AddNavMeshData(navMeshData);
    }
}
