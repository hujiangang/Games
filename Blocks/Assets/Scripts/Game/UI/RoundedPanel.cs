using UnityEngine;
using UnityEngine.UI;

/// <summary>Small vector UI panel; no extra textures or material instances.</summary>
public class RoundedPanel : Image
{
    public float radius=24;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();float corner=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
        vh.AddVert(r.center,color,Vector2.zero);
        const int steps=8;int count=0;
        for(int c=0;c<4;c++) {
            var center=new Vector2(c<2?r.xMax-corner:r.xMin+corner,c==0||c==3?r.yMax-corner:r.yMin+corner);
            for(int i=0;i<=steps;i++) {
                float angle=(90-c*90-i*90f/steps)*Mathf.Deg2Rad;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*corner,color,Vector2.zero);count++;
            }
        }
        for(int i=1;i<=count;i++) vh.AddTriangle(0,i,i==count?1:i+1);
    }
}
