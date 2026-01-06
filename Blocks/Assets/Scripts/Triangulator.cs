using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 耳切法工具类
/// </summary>
public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();
    public Triangulator(Vector2[] points) { m_points = new List<Vector2>(points); }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();
        int n = m_points.Count;
        if (n < 3) return indices.ToArray();
        int[] V = new int[n];
        if (Area() > 0) for (int v = 0; v < n; v++) V[v] = v;
        else for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
        int nv = n; int count = 2 * nv;
        for (int v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0) return indices.ToArray();
            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int w = v + 1; if (nv <= w) w = 0;
            if (Snip(u, v, w, nv, V))
            {
                int s, t;
                indices.Add(V[u]); indices.Add(V[v]); indices.Add(V[w]);
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--; count = 2 * nv;
            }
        }
        indices.Reverse(); return indices.ToArray();
    }
    private float Area()
    {
        int n = m_points.Count; float area = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++) area += m_points[p].x * m_points[q].y - m_points[q].x * m_points[p].y;
        return area * 0.5f;
    }
    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p; Vector2 A = m_points[V[u]], B = m_points[V[v]], C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x)))) return false;
        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w)) continue;
            if (InsideTriangle(A, B, C, m_points[V[p]])) return false;
        }
        return true;
    }
    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy, cCrossp, aCrossp, bCrossp;
        ax = C.x - B.x; ay = C.y - B.y; bx = A.x - C.x; by = A.y - C.y; cx = B.x - A.x; cy = B.y - A.y;
        apx = P.x - A.x; apy = P.y - A.y; bpx = P.x - B.x; bpy = P.y - B.y; cpx = P.x - C.x; cpy = P.y - C.y;
        aCrossp = ax * bpy - ay * bpx; cCrossp = cx * apy - cy * apx; bCrossp = bx * cpy - by * cpx;
        return ((aCrossp >= 0.0f) && (bCrossp >= 0.0f) && (cCrossp >= 0.0f));
    }
}