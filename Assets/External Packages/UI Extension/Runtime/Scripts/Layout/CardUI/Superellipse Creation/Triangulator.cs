﻿/// <summary>
/// Credit - ryanslikesocool 
/// Sourced from - https://github.com/ryanslikesocool/Unity-Card-UI
/// </summary>

using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    /// Credit where credit is due
    /// https://wiki.unity3d.com/index.php?title=Triangulator
    [ExecuteInEditMode]
    public class Triangulator
    {
        private readonly List<Vector2> m_points = new();

        public Triangulator(Vector2[] points) => m_points = new List<Vector2>(points);

        public int[] Triangulate()
        {
            var indices = new List<int>();

            var n = m_points.Count;
            if (n < 3) return indices.ToArray();

            var V = new int[n];
            if (Area() > 0)
                for (var v = 0; v < n; v++)
                    V[v] = v;
            else
                for (var v = 0; v < n; v++)
                    V[v] = n - 1 - v;

            var nv = n;
            var count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if (count-- <= 0) return indices.ToArray();

                var u = v;
                if (nv <= u) u = 0;
                v = u + 1;
                if (nv <= v) v = 0;
                var w = v + 1;
                if (nv <= w) w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            var n = m_points.Count;
            var A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                var pval = m_points[p];
                var qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }

            return A * 0.5f;
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            var A = m_points[V[u]];
            var B = m_points[V[v]];
            var C = m_points[V[w]];
            if (Mathf.Epsilon > (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x)) return false;
            for (p = 0; p < n; p++)
            {
                if (p == u || p == v || p == w) continue;
                var P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P)) return false;
            }

            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x;
            ay = C.y - B.y;
            bx = A.x - C.x;
            by = A.y - C.y;
            cx = B.x - A.x;
            cy = B.y - A.y;
            apx = P.x - A.x;
            apy = P.y - A.y;
            bpx = P.x - B.x;
            bpy = P.y - B.y;
            cpx = P.x - C.x;
            cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return aCROSSbp >= 0.0f && bCROSScp >= 0.0f && cCROSSap >= 0.0f;
        }
    }
}