using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer),typeof(PolygonCollider2D))]
public class PuzzlePiece : MonoBehaviour
{
    public List<Vector2> points,originPoints;
    public Vector3 correctWorldPos;
    private Mesh ownedMesh;
    private Material ownedMaterial;
    public void Init(List<Vector2> vertices,Material material,Color color,int sortingOrder)
    {
        originPoints=new List<Vector2>(vertices);
        Vector2 center=Vector2.zero;
        foreach(var p in vertices) center+=p;
        center/=vertices.Count;
        points=new List<Vector2>();
        foreach(var p in vertices) points.Add(p-center);
        transform.position=center;
        ApplyMaterial(material,color,sortingOrder);
        UpdateMesh();
    }
    void ApplyMaterial(Material material,Color color,int order)
    {
        if(ownedMaterial) Destroy(ownedMaterial);
        ownedMaterial=new Material(material);
        ownedMaterial.color=color;
        var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterial=ownedMaterial;renderer.sortingOrder=order;
    }
    public void Init_levelEdit(List<Vector2> vertices,Material material,Color color)
    { points=new List<Vector2>(vertices);ApplyMaterial(material,color,2);UpdateMesh(); }
    public void Init_Preview(List<Vector2> vertices,Material material,Color color) => Init_levelEdit(vertices,material,color);
    public void UpdateMeshWithAA() => UpdateMesh();
    public void UpdateMesh()
    {
        if(ownedMesh) Destroy(ownedMesh);
        ownedMesh=new Mesh { name="Puzzle polygon" };
        var vertices=new Vector3[points.Count];var colors=new Color[points.Count];
        for(int i=0;i<points.Count;i++) { vertices[i]=points[i];colors[i]=Color.white; }
        ownedMesh.vertices=vertices;ownedMesh.colors=colors;
        ownedMesh.triangles=new Triangulator(points.ToArray()).Triangulate();
        ownedMesh.RecalculateNormals();ownedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh=ownedMesh;
        GetComponent<PolygonCollider2D>().SetPath(0,points.ToArray());
    }
    void OnDestroy() { if(ownedMesh) Destroy(ownedMesh);if(ownedMaterial) Destroy(ownedMaterial); }
}
