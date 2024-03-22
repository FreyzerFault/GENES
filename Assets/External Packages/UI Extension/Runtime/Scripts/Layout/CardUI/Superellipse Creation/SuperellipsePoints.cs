/// <summary>
/// Credit - ryanslikesocool 
/// Sourced from - https://github.com/ryanslikesocool/Unity-Card-UI
/// </summary>

using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    /// The formula for a basic superellipse is
    /// Mathf.Pow(Mathf.Abs(x / a), n) + Mathf.Pow(Mathf.Abs(y / b), n) = 1
    [ExecuteInEditMode]
    public class SuperellipsePoints : MonoBehaviour
    {
        public float xLimits = 1f;
        public float yLimits = 1f;

        [Range(1f, 96f)] public float superness = 4f;

        [Space] [Range(1, 32)] public int levelOfDetail = 4;

        [Space] public Material material;

        private readonly List<Vector2> pointList = new();

        private int lastLoD;
        private float lastSuper;

        private float lastXLim;
        private float lastYLim;

        private void Start()
        {
            RecalculateSuperellipse();

            GetComponent<MeshRenderer>().material = material;

            lastXLim = xLimits;
            lastYLim = yLimits;
            lastSuper = superness;

            lastLoD = levelOfDetail;
        }

        private void Update()
        {
            if (lastXLim != xLimits || lastYLim != yLimits || lastSuper != superness || lastLoD != levelOfDetail)
                RecalculateSuperellipse();

            lastXLim = xLimits;
            lastYLim = yLimits;
            lastSuper = superness;

            lastLoD = levelOfDetail;
        }

        private void RecalculateSuperellipse()
        {
            pointList.Clear();

            float realLoD = levelOfDetail * 4;

            for (float i = 0; i < xLimits; i += 1 / realLoD)
            {
                var y = Superellipse(xLimits, yLimits, i, superness);
                var tempVecTwo = new Vector2(i, y);
                pointList.Add(tempVecTwo);
            }

            pointList.Add(new Vector2(xLimits, 0));
            pointList.Add(Vector2.zero);

            GetComponent<MeshCreator>().CreateMesh(pointList);
        }

        private float Superellipse(float a, float b, float x, float n)
        {
            var alpha = Mathf.Pow(x / a, n);
            var beta = 1 - alpha;
            var y = Mathf.Pow(beta, 1 / n) * b;

            return y;
        }
    }
}