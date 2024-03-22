/// Credit Farfarer
/// Sourced from - https://gist.github.com/Farfarer/a765cd07920d48a8713a0c1924db6d70
/// Updated for UI / 2D - SimonDarksideJ

using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    [Serializable]
    public class CableCurve
    {
        private static Vector2[] emptyCurve = { new(0.0f, 0.0f), new(0.0f, 0.0f) };

        [SerializeField] private Vector2 m_start;

        [SerializeField] private Vector2 m_end;

        [SerializeField] private float m_slack;

        [SerializeField] private int m_steps;

        [SerializeField] private bool m_regen;

        [SerializeField] private Vector2[] points;

        public CableCurve()
        {
            points = emptyCurve;
            m_start = Vector2.up;
            m_end = Vector2.up + Vector2.right;
            m_slack = 0.5f;
            m_steps = 20;
            m_regen = true;
        }

        public CableCurve(Vector2[] inputPoints)
        {
            points = inputPoints;
            m_start = inputPoints[0];
            m_end = inputPoints[1];
            m_slack = 0.5f;
            m_steps = 20;
            m_regen = true;
        }

        public CableCurve(List<Vector2> inputPoints)
        {
            points = inputPoints.ToArray();
            m_start = inputPoints[0];
            m_end = inputPoints[1];
            m_slack = 0.5f;
            m_steps = 20;
            m_regen = true;
        }

        public CableCurve(CableCurve v)
        {
            points = v.Points();
            m_start = v.start;
            m_end = v.end;
            m_slack = v.slack;
            m_steps = v.steps;
            m_regen = v.regenPoints;
        }

        public bool regenPoints
        {
            get => m_regen;
            set => m_regen = value;
        }

        public Vector2 start
        {
            get => m_start;
            set
            {
                if (value != m_start) m_regen = true;
                m_start = value;
            }
        }

        public Vector2 end
        {
            get => m_end;
            set
            {
                if (value != m_end) m_regen = true;
                m_end = value;
            }
        }

        public float slack
        {
            get => m_slack;
            set
            {
                if (value != m_slack) m_regen = true;
                m_slack = Mathf.Max(0.0f, value);
            }
        }

        public int steps
        {
            get => m_steps;
            set
            {
                if (value != m_steps) m_regen = true;
                m_steps = Mathf.Max(2, value);
            }
        }

        public Vector2 midPoint
        {
            get
            {
                var mid = Vector2.zero;
                if (m_steps == 2) return (points[0] + points[1]) * 0.5f;

                if (m_steps > 2)
                {
                    var m = m_steps / 2;
                    if (m_steps % 2 == 0)
                        mid = (points[m] + points[m + 1]) * 0.5f;
                    else
                        mid = points[m];
                }

                return mid;
            }
        }

        public Vector2[] Points()
        {
            if (!m_regen) return points;

            if (m_steps < 2) return emptyCurve;

            var lineDist = Vector2.Distance(m_end, m_start);
            var lineDistH = Vector2.Distance(new Vector2(m_end.x, m_start.y), m_start);
            var l = lineDist + Mathf.Max(0.0001f, m_slack);
            var r = 0.0f;
            var s = m_start.y;
            var u = lineDistH;
            var v = end.y;

            if (u - r == 0.0f) return emptyCurve;

            var ztarget = Mathf.Sqrt(Mathf.Pow(l, 2.0f) - Mathf.Pow(v - s, 2.0f)) / (u - r);

            var loops = 30;
            var iterationCount = 0;
            var maxIterations = loops * 10; // For safety.
            var found = false;

            var z = 0.0f;
            var ztest = 0.0f;
            var zstep = 100.0f;
            var ztesttarget = 0.0f;
            for (var i = 0; i < loops; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    iterationCount++;
                    ztest = z + zstep;
                    ztesttarget = (float)Math.Sinh(ztest) / ztest;

                    if (float.IsInfinity(ztesttarget)) continue;

                    if (ztesttarget == ztarget)
                    {
                        found = true;
                        z = ztest;
                        break;
                    }

                    if (ztesttarget > ztarget) break;
                    z = ztest;

                    if (iterationCount > maxIterations)
                    {
                        found = true;
                        break;
                    }
                }

                if (found) break;

                zstep *= 0.1f;
            }

            var a = (u - r) / 2.0f / z;
            var p = (r + u - a * Mathf.Log((l + v - s) / (l - v + s))) / 2.0f;
            var q = (v + s - l * (float)Math.Cosh(z) / (float)Math.Sinh(z)) / 2.0f;

            points = new Vector2[m_steps];
            float stepsf = m_steps - 1;
            float stepf;
            for (var i = 0; i < m_steps; i++)
            {
                stepf = i / stepsf;
                var pos = Vector2.zero;
                pos.x = Mathf.Lerp(start.x, end.x, stepf);
                pos.y = a * (float)Math.Cosh((stepf * lineDistH - p) / a) + q;
                points[i] = pos;
            }

            m_regen = false;
            return points;
        }
    }
}