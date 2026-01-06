using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// 碎片.
/// </summary>
public class PuzzlePiece : MonoBehaviour
{
    public List<Vector2> points; // 碎片的顶点列表
    public Material mat;

    public void Init(List<Vector2> newPoints, Material m)
    {
        points = newPoints;
        mat = m;
        UpdateMesh();
    }

    public void UpdateMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
        if (pc == null) pc = gameObject.AddComponent<PolygonCollider2D>();

        mr.material = mat;
        mr.material.color = new Color(Random.value, Random.value, Random.value); // 随机色

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++) vertices[i] = new Vector3(points[i].x, points[i].y, 0);

        Triangulator tr = new Triangulator(points.ToArray());
        mesh.vertices = vertices;
        mesh.triangles = tr.Triangulate();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        pc.SetPath(0, points.ToArray()); // 设置碰撞体，方便以后拖动
    }
}