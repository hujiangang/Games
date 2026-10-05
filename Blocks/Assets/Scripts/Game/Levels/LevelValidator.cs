using System;
using Clipper2Lib;
using UnityEngine;

public static class LevelValidator
{
    public static bool Validate(LevelData data,out string error)
    {
        error=null;
        if(data==null || data.pieces==null || data.pieces.Count<1) { error="Empty level";return false; }
        var paths=new Paths64();
        foreach(var piece in data.pieces) {
            if(piece?.vertices==null || piece.vertices.Count<3) { error="Invalid polygon";return false; }
            foreach(var v in piece.vertices)
                if(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y)) { error="Non-finite vertex";return false; }
            var path=PuzzleGeometry.Path(piece.vertices,Matrix4x4.identity);
            if(PuzzleGeometry.Area(new Paths64{path})<.01) { error="Degenerate polygon";return false; }
            int[] triangles=new Triangulator(piece.vertices.ToArray()).Triangulate();
            if(triangles.Length!=(piece.vertices.Count-2)*3) { error="Polygon cannot triangulate";return false; }
            double triangleArea=0;
            for(int i=0;i<triangles.Length;i+=3) {
                Vector2 a=piece.vertices[triangles[i]],b=piece.vertices[triangles[i+1]],c=piece.vertices[triangles[i+2]];
                triangleArea+=Math.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5;
            }
            if(Math.Abs(triangleArea-PuzzleGeometry.Area(new Paths64{path}))>.0002) { error="Triangulation area mismatch";return false; }
            paths.Add(path);
        }
        float half=data.boardHalfSize>0?data.boardHalfSize:2;
        if(!PuzzleGeometry.IsComplete(paths,PuzzleGeometry.Frame(new Rect(-half,-half,half*2,half*2)),data.pieces.Count))
        { error="Gaps, overlaps or out-of-frame polygons";return false; }
        return true;
    }
}
