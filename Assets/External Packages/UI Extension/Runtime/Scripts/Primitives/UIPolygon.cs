/// Credit CiaccoDavide
/// Sourced from - http://ciaccodavi.de/unity/UIPolygon

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Primitives/UI Polygon")]
    public class UIPolygon : UIPrimitiveBase
    {
        public bool fill = true;
        public float thickness = 5;

        [Range(3, 360)] public int sides = 3;

        [Range(0, 360)] public float rotation;

        [Range(0, 1)] public float[] VerticesDistances = new float[3];

        private float size;

        private void Update()
        {
            size = rectTransform.rect.width;
            if (rectTransform.rect.width > rectTransform.rect.height)
                size = rectTransform.rect.height;
            else
                size = rectTransform.rect.width;
            thickness = Mathf.Clamp(thickness, 0, size / 2);
        }

        public void DrawPolygon(int _sides)
        {
            sides = _sides;
            VerticesDistances = new float[_sides + 1];
            for (var i = 0; i < _sides; i++) VerticesDistances[i] = 1;
            ;
            rotation = 0;
            SetAllDirty();
        }

        public void DrawPolygon(int _sides, float[] _VerticesDistances)
        {
            sides = _sides;
            VerticesDistances = _VerticesDistances;
            rotation = 0;
            SetAllDirty();
        }

        public void DrawPolygon(int _sides, float[] _VerticesDistances, float _rotation)
        {
            sides = _sides;
            VerticesDistances = _VerticesDistances;
            rotation = _rotation;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var prevX = Vector2.zero;
            var prevY = Vector2.zero;
            var uv0 = new Vector2(0, 0);
            var uv1 = new Vector2(0, 1);
            var uv2 = new Vector2(1, 1);
            var uv3 = new Vector2(1, 0);
            Vector2 pos0;
            Vector2 pos1;
            Vector2 pos2;
            Vector2 pos3;
            var degrees = 360f / sides;
            var vertices = sides + 1;
            if (VerticesDistances.Length != vertices)
            {
                VerticesDistances = new float[vertices];
                for (var i = 0; i < vertices - 1; i++) VerticesDistances[i] = 1;
            }

            // last vertex is also the first!
            VerticesDistances[vertices - 1] = VerticesDistances[0];
            for (var i = 0; i < vertices; i++)
            {
                var outer = -rectTransform.pivot.x * size * VerticesDistances[i];
                var inner = -rectTransform.pivot.x * size * VerticesDistances[i] + thickness;
                var rad = Mathf.Deg2Rad * (i * degrees + rotation);
                var c = Mathf.Cos(rad);
                var s = Mathf.Sin(rad);
                uv0 = new Vector2(0, 1);
                uv1 = new Vector2(1, 1);
                uv2 = new Vector2(1, 0);
                uv3 = new Vector2(0, 0);
                pos0 = prevX;
                pos1 = new Vector2(outer * c, outer * s);
                if (fill)
                {
                    pos2 = Vector2.zero;
                    pos3 = Vector2.zero;
                }
                else
                {
                    pos2 = new Vector2(inner * c, inner * s);
                    pos3 = prevY;
                }

                prevX = pos1;
                prevY = pos2;
                vh.AddUIVertexQuad(SetVbo(new[] { pos0, pos1, pos2, pos3 }, new[] { uv0, uv1, uv2, uv3 }));
            }
        }
    }
}