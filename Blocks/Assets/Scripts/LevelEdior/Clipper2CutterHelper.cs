using Clipper2Lib;
using System.Collections.Generic;
using UnityEngine;

public static class Clipper2CutterHelper
{
    public static List<List<Vector2>> CutPolygon(List<Vector2> polygon, List<Vector2> path, float cutWidth = 0.015f)
    {
        // 1. 设置主体
        PathD subjPath = new PathD();
        foreach (var v in polygon) subjPath.Add(new PointD(v.x, v.y));
        PathsD subj = new PathsD { subjPath };

        // 2. 设置划线
        PathD clipLine = new PathD();
        foreach (var v in path) clipLine.Add(new PointD(v.x, v.y));
        PathsD clipPaths = new PathsD { clipLine };

        // 3. 膨胀路径（把线变成有厚度的长条）
        // EndType.Square 很重要，它能确保线头线尾多伸出去一点，彻底切断边界
        PathsD inflatedClip = Clipper.InflatePaths(clipPaths, cutWidth / 2.0, JoinType.Miter, EndType.Square);

        // 4. 执行差集运算 (Subject - Clip)
        // 使用 EvenOdd 填充规则，对于自相交折线更稳健
        PathsD solution = Clipper.BooleanOp(ClipType.Difference, subj, inflatedClip, FillRule.EvenOdd);

        // 5. 【核心修复】：返回所有分离的区域
        List<List<Vector2>> results = new List<List<Vector2>>();
        foreach (var solPath in solution)
        {
            // 过滤掉因为膨胀产生的微小细条（碎片面积太小则忽略）
            if (Clipper.Area(solPath) < 0.01) continue; 

            List<Vector2> pts = new List<Vector2>();
            foreach (var pt in solPath) pts.Add(new Vector2((float)pt.x, (float)pt.y));
            
            if (pts.Count >= 3) results.Add(pts);
        }
        return results;
    }
}