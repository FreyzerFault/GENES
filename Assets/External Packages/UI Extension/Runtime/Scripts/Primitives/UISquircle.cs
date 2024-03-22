/// Credit Soprachev Andrei

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Primitives/Squircle")]
    public class UISquircle : UIPrimitiveBase
    {
        public enum Type
        {
            Classic,
            Scaled
        }

        private const float C = 1.0f;

        [Space] public Type squircleType = Type.Scaled;

        [Range(1, 40)] public float n = 4;

        [Min(0.1f)] public float delta = 5f;

        public float quality = 0.1f;

        [Min(0)] public float radius = 1000;
        private readonly List<Vector2> vert = new();


        private float a, b;


        private float SquircleFunc(float t, bool xByY)
        {
            if (xByY) return (float)Math.Pow(C - Math.Pow(t / a, n), 1f / n) * b;

            return (float)Math.Pow(C - Math.Pow(t / b, n), 1f / n) * a;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            float dx = 0;
            float dy = 0;

            var width = rectTransform.rect.width / 2;
            var height = rectTransform.rect.height / 2;

            if (squircleType == Type.Classic)
            {
                a = width;
                b = height;
            }
            else
            {
                a = Mathf.Min(width, height, radius);
                b = a;

                dx = width - a;
                dy = height - a;
            }


            float x = 0;
            float y = 1;
            vert.Clear();
            vert.Add(new Vector2(0, height));
            while (x < y)
            {
                y = SquircleFunc(x, true);
                vert.Add(new Vector2(dx + x, dy + y));
                x += delta;
            }

            if (float.IsNaN(vert.Last().y)) vert.RemoveAt(vert.Count - 1);

            while (y > 0)
            {
                x = SquircleFunc(y, false);
                vert.Add(new Vector2(dx + x, dy + y));
                y -= delta;
            }

            vert.Add(new Vector2(width, 0));

            for (var i = 1; i < vert.Count - 1; i++)
                if (vert[i].x < vert[i].y)
                {
                    if (vert[i - 1].y - vert[i].y < quality)
                    {
                        vert.RemoveAt(i);
                        i -= 1;
                    }
                }
                else
                {
                    if (vert[i].x - vert[i - 1].x < quality)
                    {
                        vert.RemoveAt(i);
                        i -= 1;
                    }
                }

            vert.AddRange(vert.AsEnumerable().Reverse().Select(t => new Vector2(t.x, -t.y)));
            vert.AddRange(vert.AsEnumerable().Reverse().Select(t => new Vector2(-t.x, t.y)));

            vh.Clear();

            for (var i = 0; i < vert.Count - 1; i++)
            {
                vh.AddVert(vert[i], color, Vector2.zero);
                vh.AddVert(vert[i + 1], color, Vector2.zero);
                vh.AddVert(Vector2.zero, color, Vector2.zero);

                vh.AddTriangle(i * 3, i * 3 + 1, i * 3 + 2);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(UISquircle))]
        public class UISquircleEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                var script = (UISquircle)target;
                GUILayout.Label("Vertex count: " + script.vert.Count());
            }
        }
#endif
    }
}