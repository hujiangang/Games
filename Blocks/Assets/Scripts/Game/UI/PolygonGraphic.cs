using UnityEngine;
using UnityEngine.UI;

/// <summary>UI-native polygon preview; no colliders or world-space mesh objects.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PolygonGraphic : MaskableGraphic
{
    private PieceData piece;
    private float halfSize;
    public void Set(PieceData value,float half) { piece=value;halfSize=half;raycastTarget=false;SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(piece==null || piece.vertices==null) return;
        float scale=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/(2*halfSize);
        foreach(var p in piece.vertices) vh.AddVert(p*scale,piece.color,Vector2.zero);
        var triangles=new Triangulator(piece.vertices.ToArray()).Triangulate();
        for(int i=0;i<triangles.Length;i+=3) vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
    }
}
