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

    public void Init_levelEdit(List<Vector2> newPoints, Material mat)
    {
        this.points = newPoints;
        GetComponent<MeshRenderer>().material = mat;
        // 给个随机颜色方便区分
        pieceColor = new Color(Random.value, Random.value, Random.value);
        GetComponent<MeshRenderer>().material.color = pieceColor;
        GetComponent<MeshRenderer>().sortingOrder = 2;
        UpdateMesh();
    }
    
    public void UpdateMeshWithAA() {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        // 1. 计算几何中心 (作为中心点)
        Vector2 center = Vector2.zero;
        foreach (var p in points) center += p;
        center /= points.Count;

        // 2. 准备顶点：中心点(Index 0) + 边缘点(Index 1...N)
        Vector3[] vertices = new Vector3[points.Count + 1];
        Vector2[] uvs = new Vector2[points.Count + 1]; // 我们用 UV.x 存储到边缘的距离

        vertices[0] = new Vector3(center.x, center.y, 0);
        uvs[0] = new Vector2(1, 0); // 中心点，UV 设为 1

        for (int i = 0; i < points.Count; i++) {
            vertices[i + 1] = new Vector3(points[i].x, points[i].y, 0);
            uvs[i + 1] = new Vector2(0, 0); // 边缘点，UV 设为 0
        }

        // 3. 构建三角形
        int[] triangles = new int[points.Count * 3];
        for (int i = 0; i < points.Count; i++) {
            triangles[i * 3] = 0; // 中心点
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1 == points.Count) ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    public void UpdateMesh() {
        
        Mesh mesh = new();

         // 凹多边形不能有中心点，直接转换顶点
        Vector3[] vertices = new Vector3[points.Count];
        Vector2[] uvs = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++) {
            vertices[i] = new Vector3(points[i].x, points[i].y, 0);
            uvs[i] = points[i]; // 简单贴图坐标
        }

        Triangulator tr = new Triangulator(points.ToArray());
        mesh.vertices = vertices;
        //mesh.uv = uvs;
        mesh.triangles = tr.Triangulate();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        
        // 更新碰撞体
        PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
        //pc.pathCount = 1;
        pc.SetPath(0, points.ToArray());

        //AddSmoothOutline();
        // 优化锯齿效果.
        void AddSmoothOutline() {
            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr == null) lr = gameObject.AddComponent<LineRenderer>();
            
            lr.startWidth = 0.025f; // 线条不要太粗
            lr.endWidth = 0.025f;
            lr.loop = true;
            lr.useWorldSpace = false;
            
            // 关键：增加转角顶点，让线条变圆润，消除视觉锯齿
            lr.numCornerVertices = 5;
            lr.numCapVertices = 5;

            // 材质建议使用 Sprites/Default，颜色给一个半透明的深灰色
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0, 0, 0, 0.4f); // 40%透明的黑边

            Vector3[] positions = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) {
                // 稍微在 Z 轴往前放一点点 (-0.01)，确保线盖在图形边缘上
                positions[i] = new Vector3(points[i].x, points[i].y, -0.01f);
            }
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
        }
    }
}