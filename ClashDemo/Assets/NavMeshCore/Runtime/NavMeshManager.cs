using System.Collections.Generic;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Crowd;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavMeshManager : MonoBehaviour
{
    private static NavMeshManager _instance;

    public static NavMeshManager Instance
    {
        get
        {
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

    [Header("Unity NavMesh 资源")]
    public NavMeshSettings Settings; // 全局配置

    [Header("DotRecast 运行时构建")]
    public string walkableTag = "Walkable";
    public float crowdAgentRadius = 0.35f;
    public float crowdAgentHeight = 2f;
    public float crowdAgentClimb = 0.4f;
    public float crowdAgentMaxSlope = 45f;
    public float cellSize = 0.16666667f;
    public float cellHeight = 0.1f;
    public int maxVertsPerPoly = 6;
    public bool buildDetailMesh = true;

    private NavMeshData _navMeshData;
    private NavMeshDataInstance _navMeshInstance;
    private DtNavMesh _dtNavMesh;
    private DtNavMeshQuery _dtNavMeshQuery;
    private DtCrowd _crowd;
    private DtQueryDefaultFilter _queryFilter;
    private RcVec3f _queryExtents = new RcVec3f(2f, 4f, 2f);
    private bool _dotRecastReady;

    public DtCrowd Crowd => _crowd;
    public float AgentRadius => crowdAgentRadius;
    public float AgentHeight => crowdAgentHeight;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // 全局唯一
    }

    private void Update()
    {
        if (_dotRecastReady && _crowd != null)
            _crowd.Update(Time.deltaTime, null);
    }

    // 运行时加载烘焙好的数据（客户端）
    public void LoadNavMesh()
    {
        if (Settings == null)
        {
            Debug.LogError("NavMeshManager 缺少 NavMeshSettings 引用。");
            return;
        }

        _navMeshData = Resources.Load<NavMeshData>(Settings.GetResourceAssetName());
        if (_navMeshData != null)
        {
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
        }
    }

    public bool EnsureDotRecastReady()
    {
        if (_dotRecastReady && _crowd != null && _dtNavMeshQuery != null)
            return true;

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

    // 编辑器烘焙后调用（把数据存到Resources）
#if UNITY_EDITOR
    public void SaveNavMeshData(NavMeshData data)
    {
        Resources.UnloadUnusedAssets();
        AssetDatabase.CreateAsset(data, $"Assets/Resources/{Settings.GetResourceAssetName()}.asset");
        AssetDatabase.SaveAssets();
    }
#endif

    private bool BuildDotRecastFromTaggedGeometry()
    {
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

        _dtNavMesh = new DtNavMesh();
        _dtNavMesh.Init(meshData, result.Mesh.nvp, 0);
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
        return true;
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
            pathQueueSize = 32,
            maxFindPathIterations = 128,
            maxTargetFindPathIterations = 64,
            maxTopologyOptimizationIterations = 32,
            topologyOptimizationTimeThreshold = 0.5f,
            checkLookAhead = 10,
            targetReplanDelay = 0.5f,
            maxObstacleAvoidanceCircles = 6,
            maxObstacleAvoidanceSegments = 8,
            collisionResolveFactor = 0.7f
        };

        return config;
    }

    private void ConfigureObstacleAvoidance()
    {
        if (_crowd == null)
            return;

        DtObstacleAvoidanceParams obstacleParams = new DtObstacleAvoidanceParams
        {
            velBias = 0.5f,
            weightDesVel = 2f,
            weightCurVel = 0.75f,
            weightSide = 0.75f,
            weightToi = 2.5f,
            horizTime = 2.5f,
            gridSize = 33,
            adaptiveDivs = 7,
            adaptiveRings = 2,
            adaptiveDepth = 5
        };

        for (int i = 0; i < 4; i++)
            _crowd.SetObstacleAvoidanceParams(i, obstacleParams);
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
