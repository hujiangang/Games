using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class PuzzlePiece : MonoBehaviour {

    private Color pieceColor;

    public List<Vector2> points;

    public void Init(List<Vector2> newPoints, Material mat, Color color)
    {

        pieceColor = color;
        // 1. 计算这堆点的几何中心 (Centroid)
        Vector2 center = Vector2.zero;
        foreach (var p in newPoints) center += p;
        center /= newPoints.Count;

        // 2. 将所有点平移，使其围绕 (0,0) 分布
        // 这样 GameObject 的 Position 就能代表碎片的视觉中心了
        List<Vector2> centeredPoints = new List<Vector2>();
        foreach (var p in newPoints)
        {
            centeredPoints.Add(p - center);
        }

        this.points = centeredPoints;
        // 3. 将物体的世界坐标设为刚才计算的中心
        transform.position = (Vector3)center;

        GetComponent<MeshRenderer>().material = mat;
        // 给个随机颜色方便区分
        //pieceColor = new Color(Random.value, Random.value, Random.value);
        GetComponent<MeshRenderer>().material.color = pieceColor;
        GetComponent<MeshRenderer>().sortingOrder = 2;
        UpdateMesh();
    }
    
     public void Init_levelEdit(List<Vector2> newPoints, Material mat) {
        this.points = newPoints;
        GetComponent<MeshRenderer>().material = mat;
        // 给个随机颜色方便区分
        pieceColor = new Color(Random.value, Random.value, Random.value);
        GetComponent<MeshRenderer>().material.color = pieceColor;
        GetComponent<MeshRenderer>().sortingOrder = 2;
        UpdateMesh();
    }

    public void UpdateMesh() {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++) {
            vertices[i] = new Vector3(points[i].x, points[i].y, 0);
        }

        Triangulator tr = new Triangulator(points.ToArray());
        mesh.vertices = vertices;
        mesh.triangles = tr.Triangulate();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        
        // 更新碰撞体
        PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
        pc.pathCount = 1;
        pc.SetPath(0, points.ToArray());


        // 优化锯齿效果.
        void AntiAliasing()
        {
            // --- 新增：使用 LineRenderer 增加丝滑边缘 ---
            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

            // 配置线条
            lr.startWidth = 0.03f; // 线条宽度，根据需求调整
            lr.endWidth = 0.03f;
            lr.loop = true;
            lr.useWorldSpace = false; // 使用本地坐标
            lr.numCornerVertices = 5; // 让拐角圆润，消除锯齿感
            lr.numCapVertices = 5;
            
            // 设置线条颜色（比碎片颜色深一点点，或者纯白，视觉效果更好）
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = pieceColor; // 深色描边
            
            // 设置顶点
            Vector3[] linePoints = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) {
                linePoints[i] = new Vector3(points[i].x, points[i].y, -0.01f); // 稍微往前靠一点
            }
            lr.positionCount = linePoints.Length;
            lr.SetPositions(linePoints);
        }
    }
}