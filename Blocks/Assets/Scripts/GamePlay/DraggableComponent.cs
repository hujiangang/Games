using System.Collections.Generic;
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
    private static int originalOrder = 2;

    /// <summary>
    /// 吸附灵敏度.
    /// </summary>
    private float snapThreshold = 0.4f;

    private PuzzlePiece puzzlePiece;
    private Rect squareBounds;

    private Vector3 framePos;


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

    void OnMouseDown()
    {
        if (GamePlay.isGlobalLocked) return;

        isSnapped = false;

        originalOrder++;
        m_Renderer.sortingOrder = originalOrder;

        Vector3 mousePos = GetWorldMousePos();
        offset = transform.position - mousePos;
    }

    void OnMouseDrag()
    {
        if (GamePlay.isGlobalLocked) return;

        transform.position = GetWorldMousePos() + offset;
    }

    void OnMouseUp()
    {
        if (GamePlay.isGlobalLocked) return;

        if (!CheckIfFullyInside())
        {
            // 只要有一点点超出正方形，绝对不触发吸附.
            return;
        }

        // 【尝试吸附到任何边】
        if (TrySnapToAnyEdge())
        {
            // 检查是否拼成完整正方形
            FindObjectOfType<GamePlay>().CheckWinCondition2();
             isSnapped = true;
        }
    }


    bool TrySnapToAnyEdge()
    {
        // 1. 获取场景中所有已存在的边缘线段 (包括正方形边界和其他碎片)
        List<EdgeSegment> targetEdges = GetAllAvailableEdges();

        foreach (var target in targetEdges)
        {
            for (int i = 0; i < puzzlePiece.points.Count; i++)
            {
                // 1. 获取当前碎片的边线段
                Vector2 p1 = transform.TransformPoint(puzzlePiece.points[i]);
                Vector2 p2 = transform.TransformPoint(puzzlePiece.points[(i + 1) % puzzlePiece.points.Count]);

                // 2. 计算两条线的方向向量
                Vector2 dirPiece = (p2 - p1).normalized;
                Vector2 dirTarget = (target.end - target.start).normalized;

                // 3. 判断是否【平行】
                // 使用点积 (Dot Product)，如果接近 1 或 -1，说明几乎平行
                float dot = Vector2.Dot(dirPiece, dirTarget);
                if (Mathf.Abs(dot) > 0.98f)
                { // 允许 2 度左右的误差

                    // 4. 判断【垂直距离】（是否共线）
                    // 计算 p1 到目标线段所在的直线的距离
                    float dist = PointToLineDistance(p1, target.start, target.end);

                    if (dist < snapThreshold)
                    {
                        // 5. 【吸附动作】
                        // 计算目标的法线方向，并将碎片“拍”过去
                        Vector2 normal = new(-dirTarget.y, dirTarget.x);
                        float moveDist = Vector2.Dot(target.start - p1, normal);

                        transform.position += (Vector3)(normal * moveDist);

                        // 如果此时中心点已经很接近正确答案，直接完全对齐
                        if (Vector3.Distance(transform.position, correctWorldPos) < 0.5f)
                        {
                            // 不需要吸附到正确位置.
                            //transform.position = correctWorldPos;
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    float PointToLineDistance(Vector2 p, Vector2 a, Vector2 b) {
        float l2 = Vector2.SqrMagnitude(b - a);
        if (l2 == 0.0f) return Vector2.Distance(p, a);
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - a, b - a) / l2));
        Vector2 projection = a + t * (b - a);
        return Vector2.Distance(p, projection);
    }

    List<EdgeSegment> GetAllAvailableEdges()
    {
        List<EdgeSegment> edges = new();

        // A. 添加正方形的四条边 (假设位置已固定)
        float L = CutterManager.cutterLength;
        Vector3 center = framePos;
        edges.Add(new EdgeSegment(center + new Vector3(-L, L), center + new Vector3(L, L)));
        edges.Add(new EdgeSegment(center + new Vector3(L, L), center + new Vector3(L, -L)));
        edges.Add(new EdgeSegment(center + new Vector3(L, -L), center + new Vector3(-L, -L)));
        edges.Add(new EdgeSegment(center + new Vector3(-L, -L), center + new Vector3(-L, L)));

        // B. 添加其他已吸附的碎片的边缘
        foreach (var p in FindObjectsOfType<DraggableComponent>())
        {
            if (p != this && p.isSnapped)
            {
                var poly = p.GetComponent<PuzzlePiece>();
                for (int i = 0; i < poly.points.Count; i++)
                {
                    Vector2 v1 = p.transform.TransformPoint(poly.points[i]);
                    Vector2 v2 = p.transform.TransformPoint(poly.points[(i + 1) % poly.points.Count]);
                    edges.Add(new EdgeSegment(v1, v2));
                }
            }
        }
        return edges;
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
}


public struct EdgeSegment {
    public Vector2 start;
    public Vector2 end;
    public EdgeSegment(Vector2 s, Vector2 e) { start = s; end = e; }
}