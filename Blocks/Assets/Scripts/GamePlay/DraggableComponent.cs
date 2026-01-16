using System.Collections.Generic;
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
    /// 吸附灵敏度.
    /// </summary>
    private float snapThreshold = 0.8f;

    private PuzzlePiece puzzlePiece;
    private Rect squareBounds;

    private Vector3 framePos;

    /// <summary>
    /// 吸附位置修正阈值.
    /// </summary>
    private float snapCorrectPosThreshold = 0.2f;


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

        if (TrySnapToAnyEdge())
        {
            isSnapped = true;
            GameEvents.InvokeBasicEvent(GameBasicEvent.CheckFinish);
        }
    }
    #endregion


    bool TrySnapToAnyEdge()
    {
        List<EdgeSegment> allTargets = GetAllAvailableEdges();
        Vector3 originalPos = transform.position; // 记录松手时的原始位置

        float minTotalDist = float.MaxValue;
        Vector3 bestOffset = Vector3.zero;
        bool foundAnyMatch = false;

        // 遍历当前碎片的每一条边
        for (int i = 0; i < puzzlePiece.points.Count; i++)
        {
            Vector2 p1 = transform.TransformPoint(puzzlePiece.points[i]);
            Vector2 p2 = transform.TransformPoint(puzzlePiece.points[(i + 1) % puzzlePiece.points.Count]);
            Vector2 myDir = (p2 - p1).normalized;

            foreach (var target in allTargets)
            {
                Vector2 targetDir = (target.end - target.start).normalized;

                // 1. 判断是否平行
                if (Mathf.Abs(Vector2.Dot(myDir, targetDir)) > 0.999f)
                {
                    // 2. 计算两条平行线之间的垂直位移
                    Vector2 normal = new Vector2(-targetDir.y, targetDir.x);
                    float dist = Vector2.Dot(target.start - p1, normal);

                    if (Mathf.Abs(dist) < snapThreshold)
                    {
                        // 3. 【新增逻辑】线段重叠检查
                        // 如果两条线平行且距离近，但它们在水平方向上完全错开了，不应该吸附
                        if (!IsSegmentsOverlapping(p1, p2, target.start, target.end)) continue;

                        float absDist = Mathf.Abs(dist);
                        // 4. 寻找全场距离最小的匹配
                        if (absDist < minTotalDist)
                        {
                            minTotalDist = absDist;
                            bestOffset = (Vector3)(normal * dist);
                            foundAnyMatch = true;
                        }
                    }
                }
            }
        }

        if (foundAnyMatch)
        {
            // --- 第二阶段：【核心修复】重叠判定 ---

            Vector3 potentialPos = originalPos + bestOffset;

            // 1. 如果离“正确答案”极近，强制判定不重叠（信任原始关卡设计）
            if (Vector3.Distance(potentialPos, correctWorldPos) < snapCorrectPosThreshold)
            {
                transform.position = correctWorldPos;
                return true;
            }

            // 2. 模拟移动到吸附位置
            transform.position = potentialPos;

            // 3. 使用 Clipper 检查是否与已有的碎片重叠
            if (IsIntersectingWithOthers())
            {
                // 如果重叠了，弹回松手位置，拒绝吸附
                transform.position = originalPos;
                Debug.Log("拒绝吸附：该位置会导致碎片重叠");
                return false;
            }

            // 没重叠，吸附成功
            return true;
        }

        return false;
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
        const double scale = 1000.0;
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

    Vector3 GetWorldMousePos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
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