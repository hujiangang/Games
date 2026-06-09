using System.Collections.Generic;
using System.IO;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Crowd;
using DotRecast.Detour.Io;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.AI;
#endif

public class NavMeshManager : MonoBehaviour
{
    private static NavMeshManager _instance;
    private static bool _isShuttingDown;

    public static NavMeshManager Instance
    {
        get
        {
            if (_isShuttingDown)
                return null;

            if (_instance != null)
                return _instance;

            _instance = FindObjectOfType<NavMeshManager>();
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject("NavMeshManager");
            _instance = go.AddComponent<NavMeshManager>();
            return _instance;
        }
    }

    public static bool TryGetExistingInstance(out NavMeshManager manager)
    {
        manager = _instance;
        return manager != null && !_isShuttingDown;
    }

    [Header("编辑器烘焙资源")]
    public NavMeshSettings Settings; // 全局配置

    [Header("DotRecast 运行时")]
    public bool fallbackToTaggedGeometryIfRuntimeDataMissing;
    public string walkableTag = "Walkable";
    public float crowdAgentRadius = 0.35f;
    public float crowdAgentHeight = 2f;
    public float crowdAgentClimb = 0.4f;
    public float crowdAgentMaxSlope = 45f;
    public float cellSize = 0.16666667f;
    public float cellHeight = 0.1f;
    public int maxVertsPerPoly = 6;
    public bool buildDetailMesh = true;

    private DtNavMesh _dtNavMesh;
    private DtNavMeshQuery _dtNavMeshQuery;
    private DtCrowd _crowd;
    private DtQueryDefaultFilter _queryFilter;
    private RcVec3f _queryExtents = new RcVec3f(2f, 4f, 2f);
    private bool _dotRecastReady;
    private string _loadedSource;

    public DtCrowd Crowd => _crowd;
    public float AgentRadius => crowdAgentRadius;
    public float AgentHeight => crowdAgentHeight;

    void Awake()
    {
        _isShuttingDown = false;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        LoadNavMesh();
        DontDestroyOnLoad(gameObject); // 全局唯一
        
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (_dotRecastReady && _crowd != null)
            _crowd.Update(Time.deltaTime, null);
    }

    // 运行时优先加载编辑器导出的 DotRecast 数据。
    public void LoadNavMesh()
    {
        if (EnsureDotRecastReady())
            Debug.Log($"DotRecast 导航网格加载完成，来源：{_loadedSource}");
    }

    public bool EnsureDotRecastReady()
    {
        if (_dotRecastReady && _crowd != null && _dtNavMeshQuery != null)
            return true;

        if (TryLoadDotRecastFromExportedData())
            return true;

        if (!fallbackToTaggedGeometryIfRuntimeDataMissing)
        {
            Debug.LogError("DotRecast 加载失败：没有读到导出的运行时数据，且当前已关闭场景几何兜底构建。");
            return false;
        }

        Debug.LogWarning("DotRecast 未读取到导出数据，回退到场景 Walkable 几何临时构建。");
        return BuildDotRecastFromTaggedGeometry();
    }

    public bool TryRegisterAgent(Vector3 worldPosition, DtCrowdAgentParams agentParams, out DtCrowdAgent agent)
    {
        agent = null;

        if (!EnsureDotRecastReady())
            return false;

        if (!TryProjectPoint(worldPosition, out Vector3 projectedPosition, out _))
        {
            Debug.LogWarning($"DotRecast 未能在导航网格上找到靠近 {worldPosition} 的注册点。");
            return false;
        }

        agent = _crowd.AddAgent(ToRc(projectedPosition), agentParams);
        return agent != null;
    }

    public bool TryRequestMoveTarget(DtCrowdAgent agent, Vector3 worldTarget)
    {
        if (agent == null || !EnsureDotRecastReady())
            return false;

        if (!TryProjectPoint(worldTarget, out Vector3 projectedTarget, out long targetRef))
            return false;

        return _crowd.RequestMoveTarget(agent, targetRef, ToRc(projectedTarget));
    }

    public void StopAgent(DtCrowdAgent agent)
    {
        if (agent == null || _crowd == null)
            return;

        _crowd.ResetMoveTarget(agent);
        _crowd.RequestMoveVelocity(agent, RcVec3f.Zero);
    }

    public void RemoveAgent(DtCrowdAgent agent)
    {
        if (agent == null || _crowd == null)
            return;

        _crowd.RemoveAgent(agent);
    }

    public Vector3 GetAgentPosition(DtCrowdAgent agent)
    {
        if (agent == null)
            return Vector3.zero;

        return ToUnity(agent.npos);
    }

    public Vector3 GetAgentVelocity(DtCrowdAgent agent)
    {
        if (agent == null)
            return Vector3.zero;

        return ToUnity(agent.vel);
    }

    public bool TryProjectPoint(Vector3 worldPoint, out Vector3 projectedPoint, out long polyRef)
    {
        projectedPoint = worldPoint;
        polyRef = 0;

        if (!EnsureDotRecastReady())
            return false;

        RcVec3f nearestPoint = RcVec3f.Zero;
        bool isOverPoly = false;
        _dtNavMeshQuery.FindNearestPoly(ToRc(worldPoint), _queryExtents, _queryFilter, out polyRef, out nearestPoint, out isOverPoly);
        if (polyRef == 0)
            return false;

        projectedPoint = ToUnity(nearestPoint);
        return true;
    }

    // 编辑器烘焙后调用（保留给编辑器资源输出）
#if UNITY_EDITOR
    public void SaveNavMeshData(NavMeshData data)
    {
        Resources.UnloadUnusedAssets();
        AssetDatabase.CreateAsset(data, $"Assets/Resources/{Settings.GetResourceAssetName()}.asset");
        AssetDatabase.SaveAssets();
    }
#endif

    private bool TryLoadDotRecastFromExportedData()
    {
        if (Settings == null)
        {
            Debug.LogError("DotRecast 加载失败：NavMeshManager 没有关联 NavMeshSettings。");
            return false;
        }

        SyncRuntimeAgentSettings();

        string resourcePath = Settings.GetRuntimeDataResourcePath();
        TextAsset runtimeData = Resources.Load<TextAsset>(resourcePath);
        if (runtimeData == null || runtimeData.bytes == null || runtimeData.bytes.Length == 0)
        {
            Debug.LogError($"DotRecast 加载失败：Resources/{resourcePath}.bytes 不存在，请先在 NavMeshBakeWindow 执行导出。");
            return false;
        }

        try
        {
            using MemoryStream stream = new MemoryStream(runtimeData.bytes, false);
            using BinaryReader reader = new BinaryReader(stream);
            DtNavMesh navMesh = new DtMeshSetReader().Read(reader);
            if (navMesh == null)
            {
                Debug.LogError($"DotRecast 加载失败：Resources/{resourcePath}.bytes 解析结果为空。");
                return false;
            }

            InitializeRuntime(navMesh, $"Resources/{resourcePath}.bytes");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DotRecast 加载失败：读取导出数据异常。{ex.Message}");
            return false;
        }
    }

    private bool BuildDotRecastFromTaggedGeometry()
    {
        SyncRuntimeAgentSettings();

        List<float> vertices = new List<float>(2048);
        List<int> triangles = new List<int>(4096);
        CollectWalkableGeometry(vertices, triangles);

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            Debug.LogError($"DotRecast 构建失败：没有找到带 Tag `{walkableTag}` 的可行走网格。");
            return false;
        }

        RcSampleInputGeomProvider inputGeom = new RcSampleInputGeomProvider(vertices.ToArray(), triangles.ToArray());
        RcConfig config = BuildRcConfig();

        RcVec3f boundsMin = inputGeom.GetMeshBoundsMin();
        RcVec3f boundsMax = inputGeom.GetMeshBoundsMax();
        RcBuilderConfig builderConfig = new RcBuilderConfig(config, boundsMin, boundsMax);

        RcBuilderResult result = new RcBuilder().Build(inputGeom, builderConfig, true);
        if (result.Mesh == null || result.Mesh.npolys <= 0)
        {
            Debug.LogError("DotRecast 构建失败：RcBuilder 没有生成可用的多边形网格。");
            return false;
        }

        DtNavMeshCreateParams createParams = BuildCreateParams(result.Mesh, result.MeshDetail);
        DtMeshData meshData = DtNavMeshBuilder.CreateNavMeshData(createParams);
        if (meshData == null)
        {
            Debug.LogError("DotRecast 构建失败：CreateNavMeshData 返回空。");
            return false;
        }

        DtNavMesh navMesh = new DtNavMesh();
        DtStatus status = navMesh.Init(meshData, result.Mesh.nvp, 0);
        if (!status.Succeeded())
        {
            Debug.LogError($"DotRecast 构建失败：DtNavMesh.Init 返回 {status}。");
            return false;
        }

        InitializeRuntime(navMesh, $"scene tagged geometry `{walkableTag}`");
        return true;
    }

    private void SyncRuntimeAgentSettings()
    {
        if (Settings == null)
            return;

        crowdAgentRadius = Mathf.Max(0.01f, Settings.AgentRadius);
        crowdAgentHeight = Mathf.Max(0.1f, Settings.AgentHeight);
        crowdAgentClimb = Mathf.Max(cellHeight, Settings.StepHeight);
        crowdAgentMaxSlope = Settings.MaxSlope;
        maxVertsPerPoly = Mathf.Max(3, Settings.DotRecastMaxVertsPerPoly);
        cellSize = Mathf.Max(0.01f, Settings.DotRecastCellSize);
        cellHeight = Mathf.Max(0.01f, Settings.DotRecastCellHeight);
        buildDetailMesh = Settings.DotRecastBuildDetailMesh;
    }

    private void InitializeRuntime(DtNavMesh navMesh, string sourceDescription)
    {
        _dtNavMesh = navMesh;
        _dtNavMeshQuery = new DtNavMeshQuery(_dtNavMesh);
        _queryFilter = new DtQueryDefaultFilter();
        _queryExtents = new RcVec3f(
            Mathf.Max(crowdAgentRadius * 4f, 1f),
            Mathf.Max(crowdAgentHeight * 2f, 2f),
            Mathf.Max(crowdAgentRadius * 4f, 1f));

        DtCrowdConfig crowdConfig = BuildCrowdConfig();
        _crowd = new DtCrowd(crowdConfig, _dtNavMesh);
        ConfigureObstacleAvoidance();
        _dotRecastReady = true;
        _loadedSource = sourceDescription;
    }

    private void CollectWalkableGeometry(List<float> vertices, List<int> triangles)
    {
        GameObject[] walkableObjects = GameObject.FindGameObjectsWithTag(walkableTag);
        for (int i = 0; i < walkableObjects.Length; i++)
        {
            MeshFilter[] meshFilters = walkableObjects[i].GetComponentsInChildren<MeshFilter>();
            for (int j = 0; j < meshFilters.Length; j++)
            {
                Mesh mesh = meshFilters[j].sharedMesh;
                if (mesh == null)
                    continue;

                int baseVertexIndex = vertices.Count / 3;
                Matrix4x4 localToWorld = meshFilters[j].transform.localToWorldMatrix;
                Vector3[] meshVertices = mesh.vertices;
                for (int k = 0; k < meshVertices.Length; k++)
                {
                    Vector3 worldVertex = localToWorld.MultiplyPoint3x4(meshVertices[k]);
                    vertices.Add(worldVertex.x);
                    vertices.Add(worldVertex.y);
                    vertices.Add(worldVertex.z);
                }

                int[] meshTriangles = mesh.triangles;
                for (int k = 0; k < meshTriangles.Length; k++)
                    triangles.Add(baseVertexIndex + meshTriangles[k]);
            }
        }
    }

    private RcConfig BuildRcConfig()
    {
        int walkableHeight = Mathf.Max(2, Mathf.CeilToInt(crowdAgentHeight / cellHeight));
        int walkableClimb = Mathf.Max(1, Mathf.CeilToInt(crowdAgentClimb / cellHeight));
        int walkableRadius = Mathf.Max(1, Mathf.CeilToInt(crowdAgentRadius / cellSize));
        float maxEdgeLenWorld = Mathf.Max(crowdAgentRadius * 8f, 4f);
        float minRegionAreaWorld = 2f;
        float mergeRegionAreaWorld = 8f;

        return new RcConfig(
            useTiles: false,
            tileSizeX: 0,
            tileSizeZ: 0,
            borderSize: 0,
            partition: RcPartition.WATERSHED,
            cellSize: cellSize,
            cellHeight: cellHeight,
            agentMaxSlope: crowdAgentMaxSlope,
            agentHeight: crowdAgentHeight,
            agentRadius: crowdAgentRadius,
            agentMaxClimb: crowdAgentClimb,
            minRegionArea: minRegionAreaWorld,
            mergeRegionArea: mergeRegionAreaWorld,
            edgeMaxLen: maxEdgeLenWorld,
            edgeMaxError: 1.3f,
            vertsPerPoly: Mathf.Max(3, maxVertsPerPoly),
            detailSampleDist: buildDetailMesh ? cellSize * 6f : 0f,
            detailSampleMaxError: buildDetailMesh ? cellHeight : 0f,
            filterLowHangingObstacles: true,
            filterLedgeSpans: true,
            filterWalkableLowHeightSpans: true,
            walkableAreaMod: new RcAreaModification(1),
            buildMeshDetail: buildDetailMesh);
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
            walkableHeight = crowdAgentHeight,
            walkableRadius = crowdAgentRadius,
            walkableClimb = crowdAgentClimb,
            bmin = mesh.bmin,
            bmax = mesh.bmax,
            cs = mesh.cs,
            ch = mesh.ch,
            buildBvTree = true
        };
    }

    private DtCrowdConfig BuildCrowdConfig()
    {
        DtCrowdConfig config = new DtCrowdConfig(crowdAgentRadius)
        {
            pathQueueSize = 48,
            maxFindPathIterations = 128,
            maxTargetFindPathIterations = 64,
            maxTopologyOptimizationIterations = 32,
            topologyOptimizationTimeThreshold = 0.5f,
            checkLookAhead = 6,
            targetReplanDelay = 0.2f,
            maxObstacleAvoidanceCircles = 4,
            maxObstacleAvoidanceSegments = 6,
            collisionResolveFactor = 0.45f
        };

        return config;
    }

    private void ConfigureObstacleAvoidance()
    {
        if (_crowd == null)
            return;

        DtObstacleAvoidanceParams heavyPushParams = new DtObstacleAvoidanceParams
        {
            velBias = 0.72f,
            weightDesVel = 2.6f,
            weightCurVel = 0.2f,
            weightSide = 0.05f,
            weightToi = 0.75f,
            horizTime = 1f,
            gridSize = 25,
            adaptiveDivs = 5,
            adaptiveRings = 2,
            adaptiveDepth = 2
        };

        DtObstacleAvoidanceParams pushParams = new DtObstacleAvoidanceParams
        {
            velBias = 0.68f,
            weightDesVel = 2.35f,
            weightCurVel = 0.3f,
            weightSide = 0.12f,
            weightToi = 1f,
            horizTime = 1.2f,
            gridSize = 25,
            adaptiveDivs = 5,
            adaptiveRings = 2,
            adaptiveDepth = 3
        };

        DtObstacleAvoidanceParams rangedParams = new DtObstacleAvoidanceParams
        {
            velBias = 0.62f,
            weightDesVel = 2.15f,
            weightCurVel = 0.45f,
            weightSide = 0.22f,
            weightToi = 1.35f,
            horizTime = 1.5f,
            gridSize = 25,
            adaptiveDivs = 5,
            adaptiveRings = 2,
            adaptiveDepth = 3
        };

        DtObstacleAvoidanceParams cautiousParams = new DtObstacleAvoidanceParams
        {
            velBias = 0.55f,
            weightDesVel = 2f,
            weightCurVel = 0.6f,
            weightSide = 0.35f,
            weightToi = 1.75f,
            horizTime = 2f,
            gridSize = 33,
            adaptiveDivs = 7,
            adaptiveRings = 2,
            adaptiveDepth = 4
        };

        _crowd.SetObstacleAvoidanceParams(0, heavyPushParams);
        _crowd.SetObstacleAvoidanceParams(1, pushParams);
        _crowd.SetObstacleAvoidanceParams(2, rangedParams);
        _crowd.SetObstacleAvoidanceParams(3, cautiousParams);
    }

    private static RcVec3f ToRc(Vector3 worldPosition)
    {
        return new RcVec3f(worldPosition.x, worldPosition.y, worldPosition.z);
    }

    private static Vector3 ToUnity(RcVec3f value)
    {
        return new Vector3(value.X, value.Y, value.Z);
    }
}
