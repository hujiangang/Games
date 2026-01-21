using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using UnityEngine;

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


    private PuzzlePiece puzzlePiece;
    private Rect squareBounds;

    private Vector3 framePos;

    /// <summary>
    /// 正确位置吸附阈值 - 距离正确位置多近时强制吸附.
    /// </summary>
    private float snapCorrectPosThreshold = 0.3f;

    void Awake()
    {
        m_Renderer = GetComponent<MeshRenderer>();
        puzzlePiece = GetComponent<PuzzlePiece>();
    }

    public void Init(Rect rect, Vector3 correctPos, Vector3 framePos)
    {
        squareBounds = rect;
        correctWorldPos = correctPos;
        this.framePos = framePos;
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

        // 视觉提升
        globalTopOrder++;
        m_Renderer.sortingOrder = globalTopOrder;

        // 计算偏移量
        offset = (Vector2)transform.position - worldMousePos;
    }

    public void FollowMouse(Vector2 worldMousePos)
    {
        if (GamePlay.isGlobalLocked) return;
        transform.position = (Vector3)(worldMousePos + (Vector2)offset);
    }

    public void StopDragging()
    {
        if (GamePlay.isGlobalLocked) return;

        if (!CheckIfFullyInside()) return;

        Vector3 targetPos;
        if (TrySnapToAnyEdge(out targetPos))
        {
            // 直接瞬时吸附到目标位置
            transform.position = targetPos;
            isSnapped = true;
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
            // 直接吸附到正确位置
            targetPos = correctWorldPos;
            return true;
        }

        // 2. 边平行且距离近的情况.
        Vector3 twoEdgeSnapPos;
        if (CheckEdgeParallelSnap(allTargets, originalPos, out twoEdgeSnapPos))
        {
            targetPos = twoEdgeSnapPos;
            return true;
        }

        // 没有找到合适的吸附位置
        targetPos = originalPos;
        return false;
    }


    /// <summary>
    /// 检查两条边平行且距离近的吸附 - 碎片边分别匹配目标边
    /// </summary>
    private bool CheckEdgeParallelSnap(List<EdgeSegment> allTargets, Vector3 originalPos, out Vector3 snapPos)
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

            // 找到与这条碎片边最匹配的目标边
            float bestMatchScore = float.MaxValue;
            EdgeSegment bestTarget = default;
            float bestDistance = 0;

            foreach (var target in allTargets)
            {
                Vector2 targetDir = (target.end - target.start).normalized;
                // 检查是否平行
                if (Mathf.Abs(Vector2.Dot(myDir, targetDir)) >= 1f)
                {
                    // 计算两条平行线之间的垂直位移
                    Vector2 normal = new(-targetDir.y, targetDir.x);
                    float dist = Vector2.Dot(target.start - p1, normal);

                    // 检查距离和重叠
                    if (Mathf.Abs(dist) < snapThreshold &&
                        IsSegmentsOverlapping(p1, p2, target.start, target.end))
                    {
                        Debug.Log($"dist: {dist}");
                        // 计算匹配分数（距离越小分数越低）
                        float matchScore = Mathf.Abs(dist);

                        if (matchScore < bestMatchScore)
                        {
                            bestMatchScore = matchScore;
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

        // 移除平行向量的边.
        RemoveParallelNormalDuplicates(edgeMatches);

        // 计算整体的最佳移动位置
        Vector3 bestPosition = CalculateEdgeSnapPosition(edgeMatches, originalPos);

        if (bestPosition != originalPos)
        {
            // 检查重叠
            transform.position = bestPosition;
            bool wouldOverlap = IsIntersectingWithOthers();
            transform.position = originalPos;

            if (!wouldOverlap)
            {
                snapPos = bestPosition;
                return true;
            }
        }

        return false;
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
        Collider2D myCol = GetComponent<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("PuzzlePiece")); // 假设碎片都在 Pieces 层
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
        if (l2 == 0.0f) return Vector2.Distance(p, a);
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - a, b - a) / l2));
        Vector2 projection = a + t * (b - a);
        return Vector2.Distance(p, projection);
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
