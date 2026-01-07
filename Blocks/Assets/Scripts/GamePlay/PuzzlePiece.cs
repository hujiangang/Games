using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class PuzzlePiece : MonoBehaviour {
    public List<Vector2> points;

    public void Init(List<Vector2> newPoints, Material mat) {
        this.points = newPoints;
        GetComponent<MeshRenderer>().material = mat;
        // 给个随机颜色方便区分
        GetComponent<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value);
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
    }
}