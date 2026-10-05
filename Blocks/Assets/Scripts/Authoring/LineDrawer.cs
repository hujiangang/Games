using UnityEngine;
using System.Collections.Generic;

public class LineDrawer : MonoBehaviour
{
    public LineRenderer linePreview; // 用于显示轨迹
    private List<Vector2> points = new List<Vector2>();
    public Material pieceMaterial;
    private bool isEditing = false;
    public GameObject linePrefab;      // 已画完的线显示用

    public int linesortingOrder = 500;

    public List<List<Vector2>> allPaths = new();

     void DrawTargetFrame()
    {
        GameObject frame = new("TargetFrame");
        float offsetY = 0f; 
        frame.transform.position = new Vector3(0, offsetY, 0);
        LineRenderer lr = frame.AddComponent<LineRenderer>();

        lr.useWorldSpace = false;
        float lineWidth = 0.1f;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // 【核心修改】：外扩坐标
        // 为了让碎片的边缘正好对准框的“内边缘”，框的路径点要外扩 半个线宽
        float padding = 0f;
        float L = CutterManager.cutterLength + padding;
        
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(-L, L, 0.1f);
        corners[1] = new Vector3(L, L, 0.1f);
        corners[2] = new Vector3(L, -L, 0.1f);
        corners[3] = new Vector3(-L, -L, 0.1f);
        //corners[4] = new Vector3(-CutterManager.cutterLength, CutterManager.cutterLength, 0.1f); // 闭合

        lr.positionCount = 4;
        lr.SetPositions(corners);

        lr.material = pieceMaterial;
        lr.startColor = lr.endColor = new Color(0.2627451f, 0.2941177f, 0.3372549f, 1);

        lr.loop = true;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
    }

    void Start()
    {
        DrawTargetFrame();
        linePreview.GetComponent<LineRenderer>().sortingOrder = linesortingOrder;
    }

    void Update()
    {
        // 1. 左键点击：增加一个转折点（转向）
        if (Input.GetMouseButtonDown(0))
        {
            // 如果是在点 UI 按钮，不触发加点（防止穿透）
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 mousePos = GetMouseWorldPos();
            points.Add(mousePos);
            isEditing = true;
        }

        // 2. 右键点击：撤销上一个点
        if (Input.GetMouseButtonDown(1) && points.Count > 0)
        {
            points.RemoveAt(points.Count - 1);
            if (points.Count == 0) isEditing = false;
        }

        // 实时显示预览线（橡皮筋效果）
        if (isEditing)
        {
            UpdateLinePreview();
        }

        if (Input.GetMouseButtonDown(2))
        {
            if (isEditing)
            {
                FinishDrawing();
                FinishCurrentPath();
            }
        }
    }

    /// <summary>
    /// 完成当前路径，将已确定的点添加到 allPaths 中.
    /// </summary>
    void FinishCurrentPath() {
        if (points.Count >= 2) {
            allPaths.Add(new List<Vector2>(points));
            // 生成一个静态线段显示在场景中
            GameObject staticLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
            LineRenderer slr = staticLine.GetComponent<LineRenderer>();
            linesortingOrder++;
            slr.sortingOrder = linesortingOrder;
            slr.positionCount = points.Count;
            for(int i=0; i<points.Count; i++) slr.SetPosition(i, points[i]);
        }
        points.Clear();
        linePreview.positionCount = 0;
        linePreview.sortingOrder = ++linesortingOrder;
    }

    
     /// <summary>
     /// 完成一次线段绘制.
     /// </summary>
    void FinishDrawing()
    {
        isEditing = false;
        // 重新设置 LineRenderer，只保留已确定的点，去掉跟着鼠标走的那个点
        linePreview.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            linePreview.SetPosition(i, (Vector3)points[i]);
        }
    }

   void UpdateLinePreview()
    {
        // LineRenderer 显示：已固定的点 + 当前鼠标位置
        linePreview.positionCount = points.Count + 1;
        for (int i = 0; i < points.Count; i++)
        {
            linePreview.SetPosition(i, (Vector3)points[i]);
        }
        // 最后一个点始终跟着鼠标，呈现直线预览
        linePreview.SetPosition(points.Count, (Vector3)GetMouseWorldPos());
    }

    Vector2 GetMouseWorldPos()
    {
        Vector3 mp = Input.mousePosition;
        // 确保 Z 轴正确，否则 ScreenToWorldPoint 会失效
        mp.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mp);
    }

    public void ClearPath()
    {
        points.Clear();
        isEditing = false;
        linePreview.positionCount = 0;
        allPaths.Clear();
    }

    public void ClearLinePrefab()
    {
        GameObject[] lines = GameObject.FindGameObjectsWithTag("EditorLine");
        foreach (var line in lines)
        {
            Destroy(line);
        }
    }

    public void ConfirmCut()
    {
        if (allPaths.Count < 1) return;

        Debug.Log("ConfirmCut");
        
        // 执行切割
        Cutter cutter = FindObjectOfType<Cutter>();
        if (cutter != null)
        {
            cutter.ExecuteAllCuts(allPaths);
        }
        ClearPath();
        ClearLinePrefab();
    }
}