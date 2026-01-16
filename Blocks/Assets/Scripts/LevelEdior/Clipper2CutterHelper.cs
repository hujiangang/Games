using Clipper2Lib;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
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

    /// <summary>
    /// 计算多边形面积.
    /// </summary>
    /// <param name="path">多边形顶点列表.</param>
    /// <returns>多边形面积.</returns>
    public static double GetPolygonArea(List<Vector2> path)
    {
        return  Math.Abs(Clipper.Area(new PathD(path.ConvertAll(pt => new PointD(pt.x, pt.y)))));
    }


    /// <summary>
    /// 计算两个多边形的交集面积.
    /// </summary>
    /// <param name="piecePolys">碎片块多边形列表.</param>
    /// <param name="framePoints">外框多边形顶点列表.</param>
    /// <returns>两个多边形交集面积.</returns>
    public static double GetIntersectionArea(List<PolygonCollider2D> piecePolys, Vector2[] framePoints)
    {
        // 1. 设置主体
        Paths64 subjects = new(piecePolys.Count);
        foreach (var poly in piecePolys)
            subjects.Add(Path64FromPoly(poly));

        // 2. 把目标框做成 clips
        Path64 framePath = new();
        foreach (var v in framePoints) framePath.Add(new Point64(v.x, v.y));
        Paths64 clips = new Paths64(1) { framePath };

        // 3. 交集
        Paths64 intersect = Clipper.Intersect(subjects, clips, FillRule.NonZero);

        // 4. 面积累加
        double area = 0;
        foreach (var path in intersect)
            area += Math.Abs(Clipper.Area(path));

        return area;
    }
    
    public static double GetIntersectionAreaEx(List<PolygonCollider2D> piecePolys, Vector2[] framePoints)
    {
        // 1. 定义缩放倍率 (Clipper64 必须进行缩放以保留小数位精度)
        const double Scale = 1000.0;
        
        // 2. 将所有碎片顶点放大并加入 subjects
        Paths64 subjects = new Paths64(piecePolys.Count);
        foreach (var poly in piecePolys)
        {
            Path64 path = new Path64();
            foreach (var v in poly.points)
            {
                // 将本地坐标转为世界坐标，并放大
                Vector2 worldV = poly.transform.TransformPoint(v);
                path.Add(new Point64(worldV.x * Scale, worldV.y * Scale));
            }
            subjects.Add(path);
        }

        // 3. 【关键步骤】将所有碎片进行“并集”运算，合并成一个整体，并微量膨胀
        // 膨胀 0.02 单位（即 0.02 * Scale 个整数单位）来填补切割缝隙
        Paths64 combinedPieces = Clipper.Union(subjects, FillRule.NonZero);
        // 这里的 0.02 应该略大于你的 cutWidth
        Paths64 healedPieces = Clipper.InflatePaths(combinedPieces, 0.02 * Scale, JoinType.Miter, EndType.Polygon);

        // 4. 处理目标框坐标并放大
        Path64 framePath = new Path64();
        foreach (var v in framePoints) 
        {
            framePath.Add(new Point64(v.x * Scale, v.y * Scale));
        }
        Paths64 clips = new Paths64(1) { framePath };

        // 5. 计算【填补后的碎片整体】与【目标框】的交集
        Paths64 intersect = Clipper.Intersect(healedPieces, clips, FillRule.NonZero);

        // 6. 面积累加并除以缩放倍率的平方
        double totalScaledArea = 0;
        foreach (var path in intersect)
        {
            totalScaledArea += Math.Abs(Clipper.Area(path));
        }

        // 最终面积需要还原缩放：因为面积是宽*高，所以要除以 Scale 的平方
        return totalScaledArea / (Scale * Scale);
    }


    /// <summary>
    /// 从PolygonCollider2D转换为Path64.
    /// </summary>
    /// <param name="poly"></param>
    /// <returns></returns>
    private static Path64 Path64FromPoly(PolygonCollider2D poly)
    {
        Vector2[] pts = poly.points;
        Path64 path = new (pts.Length);
        Vector2 worldPos = poly.transform.position;
        foreach (var p in pts)
            path.Add(new Point64(worldPos.x + p.x, worldPos.y + p.y));
        return path;
    }

}