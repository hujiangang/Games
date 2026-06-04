using System.Collections.Generic;
using System.IO;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
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

        List<NavMeshBuildSource> sources = CollectBuildSources();

        if (sources.Count == 0)
        {
            Debug.LogWarning("NavMesh 烘焙取消：在当前打开的场景里没有找到可用于烘焙的 Walkable 几何。");
            return;
        }

        if (!TryCalculateBuildBounds(sources, out Bounds buildBounds))
        {
            Debug.LogError("NavMesh 烘焙失败：无法从当前 Build Sources 计算有效包围盒。");
            return;
        }

        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            buildSettings,
            sources,
            buildBounds,
            Vector3.zero,
            Quaternion.identity);

        if (navMeshData == null)
        {
            Debug.LogError("NavMesh 烘焙失败：BuildNavMeshData 返回了空结果。");
            return;
        }

        navMeshData.name = _settings.GetResourceAssetName();

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

        Debug.Log($"✅ 烘焙完成，NavMeshData 已保存到 {assetPath}，Sources={sources.Count}，Bounds Center={buildBounds.center}，Size={buildBounds.size}");
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
        string runtimePath = GetRuntimeBytesPath();

        WriteServerNavMeshBinary(path, tri);

        if (!TryBuildDotRecastNavMesh(out DtNavMesh dotRecastNavMesh, out string errorMessage))
        {
            Debug.LogError($"导出 DotRecast 运行时数据失败：{errorMessage}");
            return;
        }

        WriteDotRecastRuntimeBinary(runtimePath, dotRecastNavMesh);

        AssetDatabase.Refresh();
        Debug.Log($"✅ 导出完成：服务器数据 {path}，运行时数据 {runtimePath}，顶点 {tri.vertices.Length}，三角形 {tri.indices.Length / 3}");
    }

    private List<NavMeshBuildSource> CollectBuildSources()
    {
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        GameObject[] walkableRoots = GameObject.FindGameObjectsWithTag("Walkable");
        if (walkableRoots == null || walkableRoots.Length == 0)
            return sources;

        List<string> sourceNames = new List<string>();
        for (int i = 0; i < walkableRoots.Length; i++)
        {
            GameObject walkableRoot = walkableRoots[i];
            if (!walkableRoot.activeInHierarchy || !IsLayerIncluded(walkableRoot.layer, _settings.WalkableLayers))
                continue;

            MeshFilter[] meshFilters = walkableRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int j = 0; j < meshFilters.Length; j++)
            {
                MeshFilter meshFilter = meshFilters[j];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                GameObject meshObject = meshFilter.gameObject;
                if (!meshObject.activeInHierarchy || !IsLayerIncluded(meshObject.layer, _settings.WalkableLayers))
                    continue;

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null || !meshRenderer.enabled)
                    continue;

                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = meshFilter.sharedMesh,
                    transform = meshFilter.transform.localToWorldMatrix,
                    area = 0
                });

                sourceNames.Add(GetHierarchyPath(meshObject.transform));
            }
        }

        Debug.Log($"NavMesh 源采集完成：WalkableRoots={walkableRoots.Length}，Sources={sources.Count}\n{string.Join("\n", sourceNames)}");
        return sources;
    }

    private static bool IsLayerIncluded(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
            return string.Empty;

        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.name}/{path}";
        }

        return path;
    }

    private bool TryCalculateBuildBounds(List<NavMeshBuildSource> sources, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < sources.Count; i++)
        {
            if (!TryGetSourceBounds(sources[i], out Bounds sourceBounds))
                continue;

            if (!hasBounds)
            {
                bounds = sourceBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sourceBounds.min);
                bounds.Encapsulate(sourceBounds.max);
            }
        }

        return hasBounds && bounds.size.sqrMagnitude > 0f;
    }

    private bool TryGetSourceBounds(NavMeshBuildSource source, out Bounds bounds)
    {
        bounds = default;

        if (source.shape == NavMeshBuildSourceShape.Mesh && source.sourceObject is Mesh mesh)
            return TryTransformBounds(mesh.bounds, source.transform, out bounds);

        if (source.shape == NavMeshBuildSourceShape.Box)
        {
            Bounds localBounds = new Bounds(source.size * 0.5f, source.size);
            return TryTransformBounds(localBounds, source.transform, out bounds);
        }

        return false;
    }

    private bool TryTransformBounds(Bounds localBounds, Matrix4x4 localToWorld, out Bounds worldBounds)
    {
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        Vector3 firstCorner = localToWorld.MultiplyPoint3x4(corners[0]);
        worldBounds = new Bounds(firstCorner, Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            worldBounds.Encapsulate(localToWorld.MultiplyPoint3x4(corners[i]));

        return worldBounds.size.sqrMagnitude > 0f;
    }

    private void WriteServerNavMeshBinary(string path, NavMeshTriangulation tri)
    {
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new BinaryWriter(stream);

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

    private bool TryBuildDotRecastNavMesh(out DtNavMesh navMesh, out string errorMessage)
    {
        navMesh = null;
        errorMessage = null;

        List<float> vertices = new List<float>(2048);
        List<int> triangles = new List<int>(4096);
        AppendBuildSourceGeometry(vertices, triangles, CollectBuildSources());

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            errorMessage = "没有采集到可用于 DotRecast 导出的网格数据。";
            return false;
        }

        RcSampleInputGeomProvider inputGeom = new RcSampleInputGeomProvider(vertices.ToArray(), triangles.ToArray());
        RcConfig config = BuildDotRecastConfig();
        RcVec3f boundsMin = inputGeom.GetMeshBoundsMin();
        RcVec3f boundsMax = inputGeom.GetMeshBoundsMax();
        RcBuilderConfig builderConfig = new RcBuilderConfig(config, boundsMin, boundsMax);

        RcBuilderResult result = new RcBuilder().Build(inputGeom, builderConfig, true);
        if (result.Mesh == null || result.Mesh.npolys <= 0)
        {
            errorMessage = "RcBuilder 没有生成可用的多边形网格。";
            return false;
        }

        DtNavMeshCreateParams createParams = BuildCreateParams(result.Mesh, result.MeshDetail);
        DtMeshData meshData = DtNavMeshBuilder.CreateNavMeshData(createParams);
        if (meshData == null)
        {
            errorMessage = "CreateNavMeshData 返回空。";
            return false;
        }

        navMesh = new DtNavMesh();
        DtStatus status = navMesh.Init(meshData, result.Mesh.nvp, 0);
        if (!status.Succeeded())
        {
            errorMessage = $"DtNavMesh.Init 失败，状态码：{status}.";
            navMesh = null;
            return false;
        }

        return true;
    }

    private void AppendBuildSourceGeometry(List<float> vertices, List<int> triangles, List<NavMeshBuildSource> sources)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            NavMeshBuildSource source = sources[i];
            if (source.shape != NavMeshBuildSourceShape.Mesh)
                continue;

            Mesh mesh = source.sourceObject as Mesh;
            if (mesh == null)
                continue;

            int baseVertexIndex = vertices.Count / 3;
            Matrix4x4 localToWorld = source.transform;
            Vector3[] meshVertices = mesh.vertices;
            for (int j = 0; j < meshVertices.Length; j++)
            {
                Vector3 worldVertex = localToWorld.MultiplyPoint3x4(meshVertices[j]);
                vertices.Add(worldVertex.x);
                vertices.Add(worldVertex.y);
                vertices.Add(worldVertex.z);
            }

            int[] meshTriangles = mesh.triangles;
            for (int j = 0; j < meshTriangles.Length; j++)
                triangles.Add(baseVertexIndex + meshTriangles[j]);
        }
    }

    private RcConfig BuildDotRecastConfig()
    {
        float cellSize = Mathf.Max(0.01f, _settings.DotRecastCellSize);
        float cellHeight = Mathf.Max(0.01f, _settings.DotRecastCellHeight);
        float agentRadius = Mathf.Max(0.01f, _settings.AgentRadius);
        float agentHeight = Mathf.Max(cellHeight, _settings.AgentHeight);
        float agentClimb = Mathf.Max(cellHeight, _settings.StepHeight);
        float maxEdgeLenWorld = Mathf.Max(agentRadius * 8f, 4f);

        return new RcConfig(
            useTiles: false,
            tileSizeX: 0,
            tileSizeZ: 0,
            borderSize: 0,
            partition: RcPartition.WATERSHED,
            cellSize: cellSize,
            cellHeight: cellHeight,
            agentMaxSlope: _settings.MaxSlope,
            agentHeight: agentHeight,
            agentRadius: agentRadius,
            agentMaxClimb: agentClimb,
            minRegionArea: 2f,
            mergeRegionArea: 8f,
            edgeMaxLen: maxEdgeLenWorld,
            edgeMaxError: 1.3f,
            vertsPerPoly: Mathf.Max(3, _settings.DotRecastMaxVertsPerPoly),
            detailSampleDist: _settings.DotRecastBuildDetailMesh ? cellSize * 6f : 0f,
            detailSampleMaxError: _settings.DotRecastBuildDetailMesh ? cellHeight : 0f,
            filterLowHangingObstacles: true,
            filterLedgeSpans: true,
            filterWalkableLowHeightSpans: true,
            walkableAreaMod: new RcAreaModification(1),
            buildMeshDetail: _settings.DotRecastBuildDetailMesh);
    }

    private DtNavMeshCreateParams BuildCreateParams(RcPolyMesh mesh, RcPolyMeshDetail detailMesh)
    {
        int[] polyFlags = new int[mesh.npolys];
        for (int i = 0; i < polyFlags.Length; i++)
            polyFlags[i] = mesh.areas[i] != 0 ? 1 : 0;

        return new DtNavMeshCreateParams
        {
            verts = mesh.verts,
            vertCount = mesh.nverts,
            polys = mesh.polys,
            polyAreas = mesh.areas,
            polyFlags = polyFlags,
            polyCount = mesh.npolys,
            nvp = mesh.nvp,
            detailMeshes = detailMesh != null ? detailMesh.meshes : null,
            detailVerts = detailMesh != null ? detailMesh.verts : null,
            detailVertsCount = detailMesh != null ? detailMesh.nverts : 0,
            detailTris = detailMesh != null ? detailMesh.tris : null,
            detailTriCount = detailMesh != null ? detailMesh.ntris : 0,
            walkableHeight = _settings.AgentHeight,
            walkableRadius = _settings.AgentRadius,
            walkableClimb = _settings.StepHeight,
            bmin = mesh.bmin,
            bmax = mesh.bmax,
            cs = mesh.cs,
            ch = mesh.ch,
            buildBvTree = true
        };
    }

    private void WriteDotRecastRuntimeBinary(string path, DtNavMesh navMesh)
    {
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new BinaryWriter(stream);
        new DtMeshSetWriter().Write(writer, navMesh, RcByteOrder.LITTLE_ENDIAN, false);
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

    private string GetRuntimeBytesPath()
    {
        string assetDirectory = "Assets/Resources/NavMesh";
        EnsureFolderExists(assetDirectory);
        return $"{assetDirectory}/{_settings.GetRuntimeDataFileName()}";
    }

    private static void RegisterPreviewNavMesh(NavMeshData navMeshData)
    {
        if (s_previewInstance.valid)
            s_previewInstance.Remove();

        s_previewInstance = NavMesh.AddNavMeshData(navMeshData);
    }
}
