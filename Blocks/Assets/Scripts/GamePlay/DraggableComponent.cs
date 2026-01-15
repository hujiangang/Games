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
    public static int globalTopOrder = 2;

    /// <summary>
    /// 吸附灵敏度.
    /// </summary>
    private float snapThreshold = 0.5f;

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
            GameEvents.InvokeBasicEvent(GameBasicEvent.CheckFinish);
            isSnapped = true;
        }
    }
    #endregion
    
    /*

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GamePlay.isGlobalLocked) return;

        isSnapped = false;

        originalOrder++;
        Debug.Log("OnPointerDown: " + transform.name + "start sortingOrder: " + m_Renderer.sortingOrder + " end sortingOrder: " + originalOrder);
        m_Renderer.sortingOrder = originalOrder;

        Vector3 mousePos = GetWorldMousePos();
        offset = transform.position - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GamePlay.isGlobalLocked) return;

        transform.position = GetWorldMousePos() + offset;
    }

    public void OnPointerUp(PointerEventData eventData)
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
    */


    bool TrySnapToAnyEdge()
    {
        // 1. 获取场景中所有目标线段
        List<EdgeSegment> targetEdges = GetAllAvailableEdges();

        // 记录水平方向(X)和垂直方向(Y)上的最小偏移
        float minOffsetX = float.MaxValue;
        float minOffsetY = float.MaxValue;

        bool snappedX = false;
        bool snappedY = false;

        // 2. 遍历当前碎片的所有边
        for (int i = 0; i < puzzlePiece.points.Count; i++)
        {
            Vector2 p1 = transform.TransformPoint(puzzlePiece.points[i]);
            Vector2 p2 = transform.TransformPoint(puzzlePiece.points[(i + 1) % puzzlePiece.points.Count]);
            Vector2 dirPiece = (p2 - p1).normalized;

            foreach (var target in targetEdges)
            {
                Vector2 dirTarget = (target.end - target.start).normalized;
                float dot = Mathf.Abs(Vector2.Dot(dirPiece, dirTarget));

                // 判断是否平行 (允许极小误差)
                if (dot > 0.999f)
                {
                    // 计算点到线的垂直向量
                    // 使用 (target.start - p1) 在法线方向上的投影
                    Vector2 normal = new Vector2(-dirTarget.y, dirTarget.x);
                    float dist = Vector2.Dot(target.start - p1, normal);

                    if (Mathf.Abs(dist) < snapThreshold)
                    {
                        Vector2 moveVec = normal * dist;

                        // 判断该吸附是偏向水平还是垂直
                        // 如果法线主要朝向左右(X)，则它是垂直边，我们要修正 X 坐标
                        if (Mathf.Abs(normal.x) > 0.9f) 
                        {
                            if (Mathf.Abs(moveVec.x) < Mathf.Abs(minOffsetX))
                            {
                                minOffsetX = moveVec.x;
                                snappedX = true;
                            }
                        }
                        // 如果法线主要朝向上下(Y)，则它是水平边，我们要修正 Y 坐标
                        else if (Mathf.Abs(normal.y) > 0.9f)
                        {
                            if (Mathf.Abs(moveVec.y) < Mathf.Abs(minOffsetY))
                            {
                                minOffsetY = moveVec.y;
                                snappedY = true;
                            }
                        }
                    }
                }
            }
        }

        // 3. 应用偏移
        Vector3 finalPos = transform.position;
        if (snappedX) finalPos.x += minOffsetX;
        if (snappedY) finalPos.y += minOffsetY;

        if (snappedX || snappedY)
        {
            transform.position = finalPos;

            // 特殊检查：如果吸附后非常接近“终点位置”，直接强制对齐
            if (Vector3.Distance(transform.position, correctWorldPos) < snapCorrectPosThreshold) // 这里的阈值可根据需要调大
            {
                transform.position = correctWorldPos;
            }
            return true;
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