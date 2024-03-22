using System.Collections.Generic;

namespace UnityEngine.UI.Extensions.Examples
{
    [RequireComponent(typeof(UILineRenderer))]
    public class LineRendererOrbit : MonoBehaviour
    {
        public GameObject OrbitGO;

        [SerializeField] private float _xAxis = 3;

        [SerializeField] private float _yAxis = 3;

        [SerializeField] private int _steps = 10;

        private Circle circle;
        private UILineRenderer lr;
        private RectTransform orbitGOrt;
        private float orbitTime;

        public float xAxis
        {
            get => _xAxis;
            set
            {
                _xAxis = value;
                GenerateOrbit();
            }
        }

        public float yAxis
        {
            get => _yAxis;
            set
            {
                _yAxis = value;
                GenerateOrbit();
            }
        }

        public int Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                GenerateOrbit();
            }
        }


        // Use this for initialization
        private void Awake()
        {
            lr = GetComponent<UILineRenderer>();
            orbitGOrt = OrbitGO.GetComponent<RectTransform>();
            GenerateOrbit();
        }

        // Update is called once per frame
        private void Update()
        {
            orbitTime = orbitTime > _steps ? orbitTime = 0 : orbitTime + Time.deltaTime;
            orbitGOrt.localPosition = circle.Evaluate(orbitTime);
        }

        private void OnValidate()
        {
            if (lr != null) GenerateOrbit();
        }

        private void GenerateOrbit()
        {
            circle = new Circle(_xAxis, _yAxis, _steps);
            var Points = new List<Vector2>();
            for (var i = 0; i < _steps; i++) Points.Add(circle.Evaluate(i));
            Points.Add(circle.Evaluate(0));
            lr.Points = Points.ToArray();
        }
    }
}