using Clipper2Lib;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Clipper2CutterHelper
{
    public static List<List<Vector2>> CutPolygon(List<Vector2> polygon, List<Vector2> path, float cutWidth = 0.0f) 
    {
        // 如果 cutWidth 非常小或为0，我们执行“无缝切割”逻辑
        // 1. 设置主体
        PathD subjPath = new PathD();
        foreach (var v in polygon) subjPath.Add(new PointD(v.x, v.y));
        PathsD subj = new PathsD { subjPath };

        // 2. 将折线路径转换为一个“巨大的切割多边形”
        // 这个多边形会把原物体分成“左/右”或“上/下”两部分
        PathsD cuttingPoly = CreateHalfPlanePolygon(path, subjPath);

        // 3. 执行切割：
        // 第一部分 = 原物体 INTERSECT 切割面 (交集)
        PathsD partA = Clipper.BooleanOp(ClipType.Intersection, subj, cuttingPoly, FillRule.EvenOdd);
        
        // 第二部分 = 原物体 DIFFERENCE 切割面 (差集)
        PathsD partB = Clipper.BooleanOp(ClipType.Difference, subj, cuttingPoly, FillRule.EvenOdd);

        // 4. 合并结果
        List<List<Vector2>> results = new List<List<Vector2>>();
        
        // 转换结果函数（过滤小碎片）
        System.Action<PathsD> addResults = (PathsD solution) => {
            foreach (var solPath in solution) {
                if (Math.Abs(Clipper.Area(solPath)) < 0.001) continue; // 过滤面积过小的杂质
                List<Vector2> pts = new List<Vector2>();
                foreach (var pt in solPath) pts.Add(new Vector2((float)pt.x, (float)pt.y));
                if (pts.Count >= 3) results.Add(pts);
            }
        };

        addResults(partA);
        addResults(partB);

        return results;
    }

    private static PathsD CreateHalfPlanePolygon(List<Vector2> path, PathD subjPath)
    {
        // 1. 获取物体的包围盒，返回类型是 RectD
        RectD bounds = Clipper.GetBounds(new PathsD { subjPath });

        // 计算一个足够大的半径，确保能包围整个物体
        double size = Math.Max(bounds.Width, bounds.Height) * 2.0;
        if (size < 100) size = 100; // 给个最小值保底

        PathD poly = new PathD();

        // 2. 将原始切割折线的所有点加入多边形
        foreach (var v in path)
        {
            poly.Add(new PointD(v.x, v.y));
        }

        // 3. 构建“大盖子”来闭合多边形
        // 取折线的起点和终点
        Vector2 start = path[0];
        Vector2 end = path[path.Count - 1];

        // 计算从起点到终点的方向向量
        Vector2 lineDir = (end - start).normalized;
        // 计算法线方向（垂直于切割线）
        Vector2 normal = new Vector2(-lineDir.y, lineDir.x);

        // 沿着法线方向向远处延伸两个点，围成一个巨大的矩形区域
        // 这就像是用一张巨大的纸覆盖住物体的一侧
        poly.Add(new PointD(end.x + normal.x * size, end.y + normal.y * size));
        poly.Add(new PointD(start.x + normal.x * size, start.y + normal.y * size));

        return new PathsD { poly };
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