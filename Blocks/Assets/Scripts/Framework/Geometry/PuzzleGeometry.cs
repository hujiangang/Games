using System;
using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

/// <summary>Shared integer geometry for placement, content validation and completion.</summary>
public static class PuzzleGeometry
{
    public const double Scale = 100000;
    public const double AreaTolerance = .0002;
    public static Path64 Path(IReadOnlyList<Vector2> points, Matrix4x4 matrix)
    {
        var path = new Path64(points.Count);
        foreach(var p in points) {
            var v=matrix.MultiplyPoint3x4(p);
            path.Add(new Point64(Math.Round(v.x*Scale),Math.Round(v.y*Scale)));
        }
        if(Clipper.Area(path)<0) path.Reverse();
        return path;
    }
    public static Paths64 Frame(Rect r) => new Paths64 { Path(new[] {
        new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),
        new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)
    },Matrix4x4.identity) };
    public static double Area(Paths64 p) => Math.Abs(Clipper.Area(p))/(Scale*Scale);
    public static bool CanPlace(Path64 p,Paths64 frame,IEnumerable<Path64> others)
    {
        if(p.Count<3 || Area(new Paths64{p})<=AreaTolerance) return false;
        if(Area(Clipper.Difference(new Paths64{p},frame,FillRule.NonZero))>AreaTolerance) return false;
        foreach(var other in others)
            if(Area(Clipper.Intersect(new Paths64{p},new Paths64{other},FillRule.NonZero))>AreaTolerance) return false;
        return true;
    }
    public static bool IsComplete(Paths64 pieces,Paths64 frame,int expectedCount)
    {
        if(expectedCount<1 || pieces.Count!=expectedCount) return false;
        double sum=0;
        foreach(var p in pieces) {
            double area=Area(new Paths64{p});
            if(area<=AreaTolerance) return false;
            sum+=area;
        }
        var union=Clipper.Union(pieces,FillRule.NonZero);
        if(sum-Area(union)>AreaTolerance) return false;
        return Area(Clipper.Xor(union,frame,FillRule.NonZero))<=AreaTolerance;
    }
    public static bool SolveNormals(Vector2 a,float da,Vector2 b,float db,out Vector2 delta)
    {
        float det=a.x*b.y-a.y*b.x;
        if(Mathf.Abs(det)<.05f) { delta=default;return false; }
        delta=new Vector2((da*b.y-a.y*db)/det,(a.x*db-da*b.x)/det);
        return true;
    }
}
