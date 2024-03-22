/// Credit Board To Bits Games 
/// Original Sourced from - https://www.youtube.com/watch?v=Or3fA-UjnwU
/// Updated and modified for UI Extensions to be more generic


namespace UnityEngine.UI.Extensions
{
    public class Circle
    {
        [SerializeField] private int steps;

        [SerializeField] private float xAxis;

        [SerializeField] private float yAxis;

        public Circle(float radius)
        {
            xAxis = radius;
            yAxis = radius;
            steps = 1;
        }

        public Circle(float radius, int steps)
        {
            xAxis = radius;
            yAxis = radius;
            this.steps = steps;
        }

        public Circle(float xAxis, float yAxis)
        {
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            steps = 10;
        }

        public Circle(float xAxis, float yAxis, int steps)
        {
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.steps = steps;
        }

        public float X
        {
            get => xAxis;
            set => xAxis = value;
        }

        public float Y
        {
            get => yAxis;
            set => yAxis = value;
        }

        public int Steps
        {
            get => steps;
            set => steps = value;
        }

        public Vector2 Evaluate(float t)
        {
            var increments = 360f / steps;
            var angle = Mathf.Deg2Rad * increments * t;
            var x = Mathf.Sin(angle) * xAxis;
            var y = Mathf.Cos(angle) * yAxis;
            return new Vector2(x, y);
        }

        public void Evaluate(float t, out Vector2 eval)
        {
            var increments = 360f / steps;
            var angle = Mathf.Deg2Rad * increments * t;
            eval.x = Mathf.Sin(angle) * xAxis;
            eval.y = Mathf.Cos(angle) * yAxis;
        }
    }
}