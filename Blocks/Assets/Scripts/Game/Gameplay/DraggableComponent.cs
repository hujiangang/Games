using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

public class DraggableComponent : MonoBehaviour
{
    public bool isSnapped;
    public int sortingOrder;
    public static int globalTopOrder=2;
    public static readonly List<DraggableComponent> Active=new();
    public float snapDistance=.32f;
    private Vector3 correctWorldPos,localGrab,dragOrigin,originalScale;
    private bool dragging,wasSnapped;
    public bool IsDragging=>dragging;
    private Rect squareBounds;
    private PuzzlePiece piece;
    private MeshRenderer rendererCache;
    public bool CanDrag => enabled && !GamePlay.isGlobalLocked && !AdService.IsBusy &&
        GetComponent<PolygonCollider2D>().enabled;
    void Awake() { piece=GetComponent<PuzzlePiece>();rendererCache=GetComponent<MeshRenderer>(); }
    void OnEnable() { if(!Active.Contains(this)) Active.Add(this); }
    void OnDisable() { Active.Remove(this);CancelDragging(); }
    public void Init(Rect rect,Vector3 correctPos,Vector3 framePos,int order)
    { squareBounds=rect;correctWorldPos=correctPos;sortingOrder=order; }
    public void SetInitialPosition(Vector3 position)
    { transform.position=position;transform.localScale=Vector3.one;isSnapped=false; }
    public void StartDragging(Vector2 point)
    {
        if(!CanDrag || dragging) return;
        dragOrigin=transform.position;originalScale=transform.localScale;wasSnapped=isSnapped;
        localGrab=transform.InverseTransformPoint(point);
        dragging=true;isSnapped=false;
        rendererCache.sortingOrder=sortingOrder=++globalTopOrder;
        if(!GamePlay.IsStartOperation) {
            GamePlay.IsStartOperation=true;
            GameEvents.InvokeBasicEvent(GameBasicEvent.StartGameOprate);
        }
        GameEvents.InvokeBasicEvent(GameBasicEvent.PieceDraggedStart);
        FollowMouse(point);
    }
    public void FollowMouse(Vector2 point)
    {
        if(!dragging) return;
        if(GamePlay.isGlobalLocked || AdService.IsBusy) { CancelDragging();return; }
        var visible=PuzzleScreen.DragBounds;
        point=new Vector2(Mathf.Clamp(point.x,visible.xMin,visible.xMax),Mathf.Clamp(point.y,visible.yMin,visible.yMax));
        Vector3 offset=-transform.TransformVector(localGrab);
        transform.position=new Vector3(point.x+offset.x,point.y+offset.y,0);
    }
    public void CancelDragging()
    {
        if(!dragging) return;
        dragging=false;transform.position=dragOrigin;transform.localScale=originalScale;isSnapped=wasSnapped;
    }
    public void StopDragging(Vector2 point)
    {
        if(!dragging) return;
        FollowMouse(point);
        if(!dragging) return;
        dragging=false;
        // Only snapping is constrained by the board. Every other visible drop keeps its position.
        if(TrySnap(out var position)) {
            transform.position=position;isSnapped=true;
            GameEvents.InvokeBasicEvent(GameBasicEvent.PieceSnapped);
        }
        GameEvents.InvokeBasicEvent(GameBasicEvent.CheckFinish);
    }
    public bool IsPositionInvalid(Vector3 position)
    {
        var others=new List<Path64>();
        foreach(var other in Active)
            if(other && other!=this && other.transform.localScale==Vector3.one)
                others.Add(other.GetWorldPath(PuzzleGeometry.Scale));
        var path=PuzzleGeometry.Path(piece.points,Matrix4x4.TRS(position,transform.rotation,Vector3.one));
        return !PuzzleGeometry.CanPlace(path,PuzzleGeometry.Frame(squareBounds),others);
    }
    bool TrySnap(out Vector3 result)
    {
        Vector3 origin=transform.position;result=origin;
        float radius=snapDistance;
        if(Camera.main && Camera.main.orthographic)
            radius=Mathf.Clamp(22*Camera.main.orthographicSize*2/Screen.height,snapDistance*.75f,snapDistance);
        if(((Vector2)(correctWorldPos-origin)).sqrMagnitude<=radius*radius && !IsPositionInvalid(correctWorldPos))
        { result=correctWorldPos;return true; }
        var edges=new List<EdgeSegment> {
            new(new(squareBounds.xMin,squareBounds.yMin),new(squareBounds.xMax,squareBounds.yMin)),
            new(new(squareBounds.xMax,squareBounds.yMin),new(squareBounds.xMax,squareBounds.yMax)),
            new(new(squareBounds.xMax,squareBounds.yMax),new(squareBounds.xMin,squareBounds.yMax)),
            new(new(squareBounds.xMin,squareBounds.yMax),new(squareBounds.xMin,squareBounds.yMin)) };
        foreach(var other in Active) {
            if(!other || other==this || !other.isSnapped) continue;
            var pts=other.piece.points;
            for(int j=0;j<pts.Count;j++) edges.Add(new(other.transform.TransformPoint(pts[j]),other.transform.TransformPoint(pts[(j+1)%pts.Count])));
        }
        var deltas=new List<Vector2>();var joints=new List<Vector2>();
        var normals=new List<Vector2>();var distances=new List<float>();
        for(int i=0;i<piece.points.Count;i++) {
            Vector2 a=transform.TransformPoint(piece.points[i]);
            Vector2 b=transform.TransformPoint(piece.points[(i+1)%piece.points.Count]);
            foreach(var edge in edges) {
                Vector2 dir=(edge.end-edge.start).normalized;
                if(Mathf.Abs(Vector2.Dot((b-a).normalized,dir))<.9995f) continue;
                Vector2 normal=new(-dir.y,dir.x);
                float distance=Vector2.Dot(edge.start-a,normal);
                if(Mathf.Abs(distance)>radius) continue;
                float amin=Mathf.Min(Vector2.Dot(a,dir),Vector2.Dot(b,dir));
                float amax=Mathf.Max(Vector2.Dot(a,dir),Vector2.Dot(b,dir));
                float bmin=Mathf.Min(Vector2.Dot(edge.start,dir),Vector2.Dot(edge.end,dir));
                float bmax=Mathf.Max(Vector2.Dot(edge.start,dir),Vector2.Dot(edge.end,dir));
                if(Mathf.Min(amax,bmax)-Mathf.Max(amin,bmin)<.08f) continue;
                deltas.Add(normal*distance);normals.Add(normal);distances.Add(distance);
                joints.Add(edge.start-a);joints.Add(edge.end-a);joints.Add(edge.start-b);joints.Add(edge.end-b);
            }
        }
        for(int i=0;i<normals.Count;i++) for(int j=i+1;j<normals.Count;j++)
            if(PuzzleGeometry.SolveNormals(normals[i],distances[i],normals[j],distances[j],out var delta)) joints.Add(delta);
        joints.Sort((a,b)=>a.sqrMagnitude.CompareTo(b.sqrMagnitude));
        deltas.Sort((a,b)=>a.sqrMagnitude.CompareTo(b.sqrMagnitude));
        joints.AddRange(deltas);
        foreach(var delta in joints) {
            if(delta.sqrMagnitude>radius*radius) continue;
            Vector3 candidate=origin+(Vector3)delta;
            if(!IsPositionInvalid(candidate)) { result=candidate;return true; }
        }
        return false;
    }
    public Path64 GetWorldPath(double scale)
    {
        if(scale==PuzzleGeometry.Scale) return PuzzleGeometry.Path(piece.points,transform.localToWorldMatrix);
        var path=new Path64();
        foreach(var p in piece.points) { var v=transform.TransformPoint(p);path.Add(new Point64(v.x*scale,v.y*scale)); }
        if(Clipper.Area(path)<0) path.Reverse();
        return path;
    }
}
public struct EdgeSegment
{
    public Vector2 start,end;
    public EdgeSegment(Vector2 a,Vector2 b) { start=a;end=b; }
}
