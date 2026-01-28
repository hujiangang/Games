using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拖拽功能.
/// </summary>
public class DraggableComponent : MonoBehaviour
{
    /// <summary>
    /// 是否已吸附.
    /// </summary>
    public bool isSnapped = false;
    private Vector3 offset;
    private Vector3 correctWorldPos;

    private MeshRenderer m_Renderer;

    /// <summary>
    /// 排序层级.
    /// </summary>
    public static int globalTopOrder = 2;

    /// <summary>
    /// 吸附灵敏度 - 两条边平行时的距离阈值.
    /// </summary>
    private float snapThreshold = 0.3f;

    /// <summary>
    /// 平行判断阈值 - 两条边夹角余弦值大于该值则视为平行.
    /// </summary>
    private static float ParallelThreshold = 0.98f;


    private PuzzlePiece puzzlePiece;
    private Rect squareBounds;

    private Vector3 framePos;
    public int sortingOrder;

    /// <summary>
    /// 正确位置吸附阈值 - 距离正确位置多近时强制吸附.
    /// </summary>
    private float snapCorrectPosThreshold = 0.3f;

    void Awake()
    {
        m_Renderer = GetComponent<MeshRenderer>();
        puzzlePiece = GetComponent<PuzzlePiece>();
    }

    public void Init(Rect rect, Vector3 correctPos, Vector3 framePos, int sortingOrder)
    {
        squareBounds = rect;
        correctWorldPos = correctPos;
        this.framePos = framePos;
        this.sortingOrder = sortingOrder;
    }


    #region input_tag

    public void StartDragging(Vector2 worldMousePos)
    {
        if (GamePlay.isGlobalLocked) return;

        if (!GamePlay.IsStartOperation)
        {
            GamePlay.IsStartOperation = true;
            GameEvents.InvokeBasicEvent(GameBasicEvent.StartGameOprate);
        }

        isSnapped = false;
        GameEvents.InvokeBasicEvent(GameBasicEvent.PieceDraggedStart);

        // 视觉提升
        globalTopOrder++;
        m_Renderer.sortingOrder = globalTopOrder;
        sortingOrder = globalTopOrder;

        // 计算偏移量
        offset = (Vector2)transform.position - worldMousePos;
    }

    public void FollowMouse(Vector2 worldMousePos)
    {
        if (GamePlay.isGlobalLocked) return;
        //transform.position = worldMousePos;

        // Smoothly move towards the target position to prevent jerky movements
        Vector3 targetPosition = (Vector3)(worldMousePos + (Vector2)offset);
        transform.position = targetPosition;
        //transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 20f); // Adjust speed as needed
    }

    public void StopDragging(Vector2 worldMousePos)
    {
        if (GamePlay.isGlobalLocked) return;

        Debug.Log("StopDragging--------------------------------------");

        // Only proceed with snapping if the majority of the piece is inside the frame
        if (!CheckIfMajorityInside()) {
            // Even if not majority inside, still update position to where mouse was released
            transform.position = (Vector3)(worldMousePos + (Vector2)offset);
            return;
        }

        Vector3 targetPos;
        if (TrySnapToAnyEdge(out targetPos))
        {
            // 直接瞬时吸附到目标位置
            transform.position = targetPos;
            isSnapped = true;
            
            // 触发吸附事件
            GameEvents.InvokeBasicEvent(GameBasicEvent.PieceSnapped);
            GameEvents.InvokeBasicEvent(GameBasicEvent.CheckFinish);
        }
    }
    #endregion

    bool TrySnapToAnyEdge(out Vector3 targetPos)
    {
        List<EdgeSegment> allTargets = GetAllAvailableEdges();
        Vector3 originalPos = transform.position; // 记录松手时的原始位置

        // 1. 首先检查是否在正确位置附近（最高优先级）
        float distanceToCorrect = Vector3.Distance(originalPos, correctWorldPos);
        if (distanceToCorrect < snapCorrectPosThreshold)
        {
            targetPos = correctWorldPos;
            if (!IsPositionInvalid(targetPos)) return true;
        }

        // 2. 边平行且距离近的情况.
        GetEdgeParallelSnap(allTargets, originalPos, out targetPos);
        if (!IsPositionInvalid(targetPos)) return true;
    
        return false;
    }

    /// <summary>
    /// 检查指定位置是否会超出边框或与其他碎片重叠
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    /// <returns>如果超出边框或与其他碎片重叠则返回true，否则返回false</returns>
    public bool IsPositionInvalid(Vector3 targetPos)
    {
        // 保存当前位置
        Vector3 originalPos = transform.position;
        
        // 临时移动到目标位置
        transform.position = targetPos;
        
        try
        {
            // 检查是否完全在边框内.
            if (!CheckIfFullyInside())
            {
                return true;
            }
            
            // 检查是否与其他已吸附的碎片重叠
            if (IsIntersectingWithOthers())
            {
                return true; // 与其它碎片重叠
            }
            
            // 位置有效
            return false;
        }
        finally
        {
            // 恢复原来的位置
            transform.position = originalPos;
        }
    }

    /// <summary>
    /// 检查两条边平行且距离近的吸附 - 碎片边分别匹配目标边
    /// </summary>
    private void GetEdgeParallelSnap(List<EdgeSegment> allTargets, Vector3 originalPos, out Vector3 snapPos)
    {
        snapPos = originalPos;

        // 1. 为当前碎片的每条边找到最佳的匹配目标边
        List<EdgeMatch> edgeMatches = new();
        Vector2 bestNormal = Vector2.zero;

        for (int i = 0; i < puzzlePiece.points.Count; i++)
        {
            Vector2 p1 = transform.TransformPoint(puzzlePiece.points[i]);
            Vector2 p2 = transform.TransformPoint(puzzlePiece.points[(i + 1) % puzzlePiece.points.Count]);
            Vector2 myDir = (p2 - p1).normalized;
            EdgeSegment currentPieceEdge = new(p1, p2);

            // 找到与这条碎片边最匹配的目标边
            float bestMatchScore = float.MaxValue;
            EdgeSegment bestTarget = default;
            float bestDistance = 0;

            foreach (var target in allTargets)
            {
                Vector2 targetDir = (target.end - target.start).normalized;
                float dotValue = Mathf.Abs(Vector2.Dot(myDir, targetDir));
                //Debug.Log($"dotValue: {dotValue}, piecePoint:({p1.x},{p1.y})->({p2.x},{p2.y}), target:({target.start.x},{target.start.y})->({target.end.x},{target.end.y})");
                // 检查是否平行
                if (dotValue >= ParallelThreshold)
                {
                    // 计算两条平行线之间的垂直位移
                    Vector2 normal = new(-targetDir.y, targetDir.x);
                    normal = normal.normalized;
                    float dist = Vector2.Dot(target.start - p1, normal);
                    float absDist = Mathf.Abs(dist);

                    // 检查距离和重叠
                    if (absDist < snapThreshold &&
                        IsSegmentsOverlapping(p1, p2, target.start, target.end))
                    {
                        if (absDist < bestMatchScore)
                        {
                            bestMatchScore = absDist;
                            bestTarget = target;
                            bestDistance = dist;
                            bestNormal = normal;
                        }
                    }
                }
            }

            // 如果找到了匹配的目标边
            if (bestMatchScore < snapThreshold)
            {
                edgeMatches.Add(new EdgeMatch
                {
                    pieceEdgeIndex = i,
                    targetEdge = bestTarget,
                    distance = bestDistance,
                    matchScore = bestMatchScore,
                    normal = bestNormal
                });
            }
        }

        edgeMatches = FilterSameVertexEdgeMatches(edgeMatches);

        // 移除平行向量的边.
        RemoveParallelNormalDuplicates(edgeMatches);

        // 计算整体的最佳移动位置
        snapPos = CalculateEdgeSnapPosition(edgeMatches, originalPos);

    }

    /// <summary>
    /// 极简版：判断两两边是否有相同顶点，找到则返回这两条，否则返回最近一条
    /// </summary>
    private List<EdgeMatch> FilterSameVertexEdgeMatches(List<EdgeMatch> edgeMatches)
    {
        List<EdgeMatch> result = new List<EdgeMatch>();
        if (edgeMatches.Count == 0) return result;

        // 1. 先按匹配分数排序（保证优先选距离近的边）
        edgeMatches.Sort((a, b) => a.matchScore.CompareTo(b.matchScore));

        // 2. 核心：遍历所有边对，判断是否有共享顶点（就是你说的“两两边判断是否有相同的点”）
        float vertexEpsilon = 0.05f; // 顶点误差容错
        EdgeMatch firstMatch = default;
        EdgeMatch secondMatch = default;
        bool found = false;

        // 遍历所有边对（i和j两两组合）
        for (int i = 0; i < edgeMatches.Count; i++)
        {
            for (int j = i + 1; j < edgeMatches.Count; j++)
            {
                EdgeMatch edge1 = edgeMatches[i];
                EdgeMatch edge2 = edgeMatches[j];

                // 提取两条边的所有顶点
                Vector2 v1_1 = edge1.targetEdge.start; // 第一条边的起点
                Vector2 v1_2 = edge1.targetEdge.end;   // 第一条边的终点
                Vector2 v2_1 = edge2.targetEdge.start; // 第二条边的起点
                Vector2 v2_2 = edge2.targetEdge.end;   // 第二条边的终点

                // 核心判断：两条边是否有任意一个顶点相同（这就是你要的逻辑！）
                bool hasSamePoint = 
                    Vector2.Distance(v1_1, v2_1) < vertexEpsilon || // 边1起点 = 边2起点
                    Vector2.Distance(v1_1, v2_2) < vertexEpsilon || // 边1起点 = 边2终点
                    Vector2.Distance(v1_2, v2_1) < vertexEpsilon || // 边1终点 = 边2起点
                    Vector2.Distance(v1_2, v2_2) < vertexEpsilon;  // 边1终点 = 边2终点

                if (hasSamePoint)
                {
                    firstMatch = edge1;
                    secondMatch = edge2;
                    found = true;
                    break; // 找到第一组符合条件的边对就停止（因为已经排过序，是最优的）
                }
            }
            if (found) break;
        }

        // 3. 结果赋值：找到则返回两条，否则返回最近一条
        if (found)
        {
            result.Add(firstMatch);
            result.Add(secondMatch);
        }
        else
        {
            result.Add(edgeMatches[0]); // 只保留距离最近的一条
        }

        return result;
    }


    private void RemoveParallelNormalDuplicates(List<EdgeMatch> edgeMatches)
    {
        if (edgeMatches.Count <= 1) return;
        List<EdgeMatch> uniqueMatches = new();
        // 浮点精度容错值（避免微小误差导致平行判断失效）
        float epsilon = 0.001f;

        foreach (var match in edgeMatches)
        {
            // 标记当前match的normal是否与已保留的任意一个平行
            bool isParallelToExisting = false;

            foreach (var uniqueMatch in uniqueMatches)
            {
                // 计算两个normal的叉积（判断是否平行）
                float crossProduct = match.normal.x * uniqueMatch.normal.y - match.normal.y * uniqueMatch.normal.x;

                // 叉积绝对值小于epsilon，说明向量平行
                if (Mathf.Abs(crossProduct) < epsilon)
                {
                    isParallelToExisting = true;
                    break;
                }
            }

            // 如果不与已保留的任何元素平行，就加入结果列表
            if (!isParallelToExisting)
            {
                uniqueMatches.Add(match);
            }
            // 可选：如果需要保留平行组中最优的（比如matchScore最高），替换下面的逻辑
            /*
            else
            {
                // 找到平行的那个元素，替换为score更高的
                for (int i = 0; i < uniqueMatches.Count; i++)
                {
                    float cross = match.normal.x * uniqueMatches[i].normal.y - match.normal.y * uniqueMatches[i].normal.x;
                    if (Mathf.Abs(cross) < epsilon)
                    {
                        // 保留matchScore更高的（也可以改成保留distance更小的）
                        if (match.matchScore > uniqueMatches[i].matchScore)
                        {
                            uniqueMatches[i] = match;
                        }
                        break;
                    }
                }
            }
            */
        }

        edgeMatches.Clear();
        edgeMatches.AddRange(uniqueMatches);
    }

    /// <summary>
    /// 计算匹配的最佳吸附位置
    /// </summary>
    private Vector3 CalculateEdgeSnapPosition(List<EdgeMatch> edgeMatches, Vector3 originalPos)
    {
        if (edgeMatches.Count == 0) return originalPos;
        if (edgeMatches.Count == 1) return originalPos + (Vector3)edgeMatches[0].normal * edgeMatches[0].distance;

        // 只处理两条边的情况（按用户要求）
        if (edgeMatches.Count >= 2)
        {
            // 按匹配分数排序，取最好的两条
            edgeMatches.Sort((a, b) => a.matchScore.CompareTo(b.matchScore));
            var matchA = edgeMatches[0];
            var matchB = edgeMatches[1];

            // 计算吸附到边A的精确位置
            Vector3 posA = originalPos + (Vector3)matchA.normal * matchA.distance;

            // 计算吸附到边B的精确位置
            Vector3 posB = originalPos + (Vector3)matchB.normal * matchB.distance;
            Vector3 snapPos = originalPos;
            float epsilon = 0.001f;


            // 第一步：基于原始位置判断，修正A的轴（核心：用originalPos对比，不是snapPos）
            if (Mathf.Abs(originalPos.x - posA.x) < epsilon)
            {
                // 原始X和A的X一致 → 修正Y为A的Y
                snapPos.y = posA.y;
            }
            else
            {
                // 原始X和A的X不一致 → 修正X为A的X
                snapPos.x = posA.x;
            }

            // 第二步：仍基于原始位置判断，修正B的轴（关键：不依赖已更新的snapPos）
            if (Mathf.Abs(originalPos.x - posB.x) < epsilon)
            {
                // 原始X和B的X一致 → 修正Y为B的Y
                snapPos.y = posB.y;
            }
            else
            {
                // 原始X和B的X不一致 → 修正X为B的X
                snapPos.x = posB.x;
            }



            Debug.Log($"posA: ({posA.x},{posA.y},{posA.z}), posB: ({posB.x},{posB.y},{posB.z}), originalPos:({originalPos.x},{originalPos.y},{originalPos.z}), snapPos :({snapPos.x},{snapPos.y},{snapPos.z})");
            // 选择最佳的最终位置
            return snapPos;
        }

        return originalPos;
    }

    /// <summary>
    /// 边匹配信息
    /// </summary>
    private struct EdgeMatch
    {
        public int pieceEdgeIndex;
        public EdgeSegment targetEdge;
        public float distance;
        public float matchScore;
        public Vector2 normal;
    }

    // 利用 Clipper2 检查当前位置是否压在了其他已吸附碎片上
    bool IsIntersectingWithOthers()
    {
        // 1. 先用 Unity 自带的 Collider 快速过滤
        PolygonCollider2D myCol = GetComponent<PolygonCollider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("PuzzlePiece"));
        List<Collider2D> results = new List<Collider2D>();

        if (myCol.OverlapCollider(filter, results) > 0)
        {
            foreach (var res in results)
            {
                var other = res.GetComponent<DraggableComponent>();
                if (other != null && other != this && other.isSnapped)
                {
                    // 2. 只有 Collider 碰到了，才调用昂贵的 Clipper 面积计算
                    if (GetOverlapAreaWith(other) > 0.05) return true;
                }
            }
        }
        return false;
    }

    bool IsIntersectingWithOthers2()
    {
        // 1. 先用 Unity 自带的 Collider 快速过滤
        PolygonCollider2D myCol = GetComponent<PolygonCollider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("PuzzlePiece"));
        List<Collider2D> results = new List<Collider2D>();

        if (myCol.OverlapCollider(filter, results) > 0)
        {
            return true;
        }
        return false;
    }

    // 辅助函数：判断两条平行线段在投影方向上是否有交集（防止隔空远距离吸附）
    bool IsSegmentsOverlapping(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 dir = (b - a).normalized;
        float aP = Vector2.Dot(a, dir);
        float bP = Vector2.Dot(b, dir);
        float cP = Vector2.Dot(c, dir);
        float dP = Vector2.Dot(d, dir);

        float min1 = Mathf.Min(aP, bP), max1 = Mathf.Max(aP, bP);
        float min2 = Mathf.Min(cP, dP), max2 = Mathf.Max(cP, dP);

        return max1 >= min2 && max2 >= min1;
    }

    // 获取两个碎片之间的精确重叠面积
    double GetOverlapAreaWith(DraggableComponent other)
    {
        const double scale = 100.0;
        Path64 pathA = this.GetWorldPath(scale);
        Path64 pathB = other.GetWorldPath(scale);

        // 执行交集运算
        Paths64 intersect = Clipper.Intersect(new Paths64 { pathA }, new Paths64 { pathB }, FillRule.NonZero);

        double area = 0;
        foreach (var path in intersect) area += System.Math.Abs(Clipper.Area(path));

        return area / (scale * scale);
    }

    float PointToLineDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        float l2 = Vector2.SqrMagnitude(b - a);
        if (l2 < Mathf.Epsilon) return Vector2.Distance(p, a);
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - a, b - a) / l2));
        Vector2 projection = a + t * (b - a);
        return Vector2.Distance(p, projection);
    }

    /// <summary>
    /// 平行线段的最短距离（优化版）
    /// </summary>
    private float ParallelSegmentDistance(EdgeSegment segA, EdgeSegment segB)
    {
        if (Vector2.Distance(segA.start, segA.end) < Mathf.Epsilon)
            return PointToLineDistance(segA.start, segB.start, segB.end);
        if (Vector2.Distance(segB.start, segB.end) < Mathf.Epsilon)
            return PointToLineDistance(segB.start, segA.start, segA.end);

        float distance = PointToLineDistance(segA.start, segB.start, segB.end);
        return Mathf.Max(distance, 0f);
    }


    List<EdgeSegment> GetAllAvailableEdges()
    {
        List<EdgeSegment> pieceEdges = new List<EdgeSegment>();
        List<EdgeSegment> frameEdges = new List<EdgeSegment>();

        // A. 先收集其他已吸附碎片的边
        foreach (var p in FindObjectsOfType<DraggableComponent>())
        {
            if (p != this && p.isSnapped)
            {
                var poly = p.GetComponent<PuzzlePiece>();
                for (int i = 0; i < poly.points.Count; i++)
                {
                    Vector2 v1 = p.transform.TransformPoint(poly.points[i]);
                    Vector2 v2 = p.transform.TransformPoint(poly.points[(i + 1) % poly.points.Count]);
                    pieceEdges.Add(new EdgeSegment(v1, v2));
                }
            }
        }

        // B. 再收集正方形边框的边
        float L = CutterManager.cutterLength;
        Vector3 center = framePos;
        frameEdges.Add(new EdgeSegment(center + new Vector3(-L, L), center + new Vector3(L, L)));
        frameEdges.Add(new EdgeSegment(center + new Vector3(L, L), center + new Vector3(L, -L)));
        frameEdges.Add(new EdgeSegment(center + new Vector3(L, -L), center + new Vector3(-L, -L)));
        frameEdges.Add(new EdgeSegment(center + new Vector3(-L, -L), center + new Vector3(-L, L)));

        // 合并列表：碎片边在前，边框边在后
        pieceEdges.AddRange(frameEdges);
        return pieceEdges;
    }




    /// <summary>
    /// 检查碎片是否有大部分在正方形内.
    /// </summary>
    /// <returns>如果大部分顶点在框内则返回true，否则返回false</returns>
    bool CheckIfMajorityInside()
    {
        if (puzzlePiece.points == null || puzzlePiece.points.Count == 0)
            return false;
        
        int insideCount = 0;
        
        foreach (Vector2 p in puzzlePiece.points)
        {
            // 将碎片的本地顶点坐标转换为世界坐标
            Vector2 worldVtx = transform.TransformPoint(p);

            // 检查顶点是否在正方形边界内
            if (worldVtx.x >= squareBounds.xMin - 0.05f && worldVtx.x <= squareBounds.xMax + 0.05f &&
                worldVtx.y >= squareBounds.yMin - 0.05f && worldVtx.y <= squareBounds.yMax + 0.05f)
            {
                insideCount++;
            }
        }
        
        // 如果超过一半的顶点在框内，则认为大部分在框内
        return insideCount > puzzlePiece.points.Count / 2;
    }

    /// <summary>
    /// 检查碎片是否大部分在正方形内 (使用面积方法).
    /// </summary>
    /// <returns>如果碎片大部分面积在框内则返回true，否则返回false</returns>
    bool CheckIfMajorityAreaInside()
    {
        // Get the world path of the current piece
        Path64 piecePath = GetWorldPath(100.0); // Use a scale for precision
        
        // Create a rectangle path representing the square bounds
        Path64 boundsPath = new Path64();
        boundsPath.Add(new Point64(squareBounds.xMin * 100.0, squareBounds.yMax * 100.0)); // Top-left
        boundsPath.Add(new Point64(squareBounds.xMax * 100.0, squareBounds.yMax * 100.0)); // Top-right
        boundsPath.Add(new Point64(squareBounds.xMax * 100.0, squareBounds.yMin * 100.0)); // Bottom-right
        boundsPath.Add(new Point64(squareBounds.xMin * 100.0, squareBounds.yMin * 100.0)); // Bottom-left
        
        // Calculate the total area of the piece
        double totalArea = System.Math.Abs(Clipper.Area(piecePath));
        
        // Calculate the intersection area between piece and bounds
        Paths64 intersection = Clipper.Intersect(new Paths64 { piecePath }, new Paths64 { boundsPath }, FillRule.NonZero);
        
        double intersectionArea = 0;
        foreach (var path in intersection) {
            intersectionArea += System.Math.Abs(Clipper.Area(path));
        }
        
        // Define threshold for "majority" (e.g., 50%)
        double majorityThreshold = 0.5; // 50% of the piece should be inside the frame
        
        return (intersectionArea / totalArea) >= majorityThreshold && totalArea > 0;
    }

    /// <summary>
    /// 检查碎片是否完全在正方形内.
    /// </summary>
    /// <returns></returns>
    bool CheckIfFullyInside()
    {
        foreach (Vector2 p in puzzlePiece.points)
        {
            // 将碎片的本地顶点坐标转换为世界坐标
            Vector2 worldVtx = transform.TransformPoint(p);

            // 如果任意一个顶点在正方形边界外，返回 false
            if (worldVtx.x < squareBounds.xMin - 0.05f || worldVtx.x > squareBounds.xMax + 0.05f ||
                worldVtx.y < squareBounds.yMin - 0.05f || worldVtx.y > squareBounds.yMax + 0.05f)
            {
                return false;
            }
        }
        return true;
    }



    public Path64 GetWorldPath(double scale)
    {
        Path64 path = new();

        // 1. 获取 PuzzlePiece 组件里的原始本地坐标列表
        // 注意：如果你之前重置过 Pivot，这里的 points 就是相对于 transform.position 的偏移
        var puzzlePiece = GetComponent<PuzzlePiece>();
        if (puzzlePiece == null || puzzlePiece.points == null) return path;

        // 2. 遍历每个点，转换为世界坐标并放大
        foreach (Vector2 localVtx in puzzlePiece.points)
        {
            // TransformPoint 会考虑物体的 position, rotation 和 scale
            Vector3 worldVtx = transform.TransformPoint(localVtx);

            // 转换为 Clipper2 的整数点
            path.Add(new Clipper2Lib.Point64(worldVtx.x * scale, worldVtx.y * scale));
        }

        return path;
    }
}


public struct EdgeSegment
{
    public Vector2 start;
    public Vector2 end;
    public EdgeSegment(Vector2 s, Vector2 e) { start = s; end = e; }
}
