using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent game controls drawn with UI geometry.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PuzzleIcon : MaskableGraphic
{
    public enum Kind { Eye, Volume, Muted, Previous, Next, Expand, Collapse, Sidebar, Video }
    private Kind kind;
    public Kind Symbol { get=>kind;set { kind=value;SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        switch(kind) {
            case Kind.Eye:
                for(int i=0;i<28;i++) {
                    float a=i/28f,b=(i+1)/28f;
                    for(int sign=-1;sign<=1;sign+=2)
                        Line(vh,new Vector2(.08f+.84f*a,.5f+sign*.25f*Mathf.Sin(a*Mathf.PI)),
                            new Vector2(.08f+.84f*b,.5f+sign*.25f*Mathf.Sin(b*Mathf.PI)));
                }
                Arc(vh,new Vector2(.5f,.5f),.13f,0,360);break;
            case Kind.Volume:
            case Kind.Muted:
                Polygon(vh,new[]{new Vector2(.13f,.36f),new Vector2(.30f,.36f),new Vector2(.49f,.20f),
                    new Vector2(.49f,.80f),new Vector2(.30f,.64f),new Vector2(.13f,.64f)});
                if(kind==Kind.Muted) {
                    Line(vh,new Vector2(.64f,.38f),new Vector2(.88f,.62f));
                    Line(vh,new Vector2(.64f,.62f),new Vector2(.88f,.38f));
                } else { Arc(vh,new Vector2(.46f,.5f),.24f,-60,60);Arc(vh,new Vector2(.46f,.5f),.40f,-60,60); }
                break;
            case Kind.Previous: Chevron(vh,new Vector2(.62f,.23f),new Vector2(.35f,.5f),new Vector2(.62f,.77f));break;
            case Kind.Next: Chevron(vh,new Vector2(.38f,.23f),new Vector2(.65f,.5f),new Vector2(.38f,.77f));break;
            case Kind.Expand: Chevron(vh,new Vector2(.22f,.35f),new Vector2(.5f,.65f),new Vector2(.78f,.35f));break;
            case Kind.Collapse: Chevron(vh,new Vector2(.22f,.65f),new Vector2(.5f,.35f),new Vector2(.78f,.65f));break;
            case Kind.Sidebar:
                Line(vh,new Vector2(.2f,.8f),new Vector2(.2f,.2f));Line(vh,new Vector2(.2f,.2f),new Vector2(.7f,.2f));
                Line(vh,new Vector2(.4f,.5f),new Vector2(.85f,.5f));
                Chevron(vh,new Vector2(.65f,.3f),new Vector2(.85f,.5f),new Vector2(.65f,.7f));break;
            case Kind.Video: Polygon(vh,new[]{new Vector2(.32f,.22f),new Vector2(.80f,.5f),new Vector2(.32f,.78f)});break;
        }
    }
    Vector2 Point(Vector2 p) { var r=rectTransform.rect;return r.min+Vector2.Scale(p,r.size); }
    void Line(VertexHelper vh,Vector2 a,Vector2 b)
    {
        a=Point(a);b=Point(b);Vector2 d=(b-a).normalized;
        Vector2 n=new Vector2(-d.y,d.x)*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.027f;
        int i=vh.currentVertCount;
        vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
        vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
    void Chevron(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c) { Line(vh,a,b);Line(vh,b,c); }
    void Arc(VertexHelper vh,Vector2 center,float radius,float start,float end)
    {
        for(int i=0;i<36;i++) {
            float a=Mathf.Lerp(start,end,i/36f)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/36f)*Mathf.Deg2Rad;
            Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);
        }
    }
    void Polygon(VertexHelper vh,Vector2[] points)
    {
        int first=vh.currentVertCount;foreach(var p in points) vh.AddVert(Point(p),color,Vector2.zero);
        var triangles=new Triangulator(points).Triangulate();
        for(int i=0;i<triangles.Length;i+=3) vh.AddTriangle(first+triangles[i],first+triangles[i+1],first+triangles[i+2]);
    }
}
