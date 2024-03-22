/// Credit Alastair Aitchison
/// Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/issues/123/uilinerenderer-issues-with-specifying

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/UI Line Connector")]
    [RequireComponent(typeof(UILineRenderer))]
    [ExecuteInEditMode]
    public class UILineConnector : MonoBehaviour
    {
        // The elements between which line segments should be drawn
        public RectTransform[] transforms;
        private RectTransform canvas;
        private UILineRenderer lr;
        private Vector3[] previousPositions;
        private RectTransform rt;

        private void Awake()
        {
            var canvasParent = GetComponentInParent<RectTransform>().GetParentCanvas();
            if (canvasParent != null) canvas = canvasParent.GetComponent<RectTransform>();
            rt = GetComponent<RectTransform>();
            lr = GetComponent<UILineRenderer>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (transforms == null || transforms.Length < 1) return;
            //Performance check to only redraw when the child transforms move
            if (previousPositions != null && previousPositions.Length == transforms.Length)
            {
                var updateLine = false;
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] == null) continue;
                    if (!updateLine && previousPositions[i] != transforms[i].position) updateLine = true;
                }

                if (!updateLine) return;
            }

            // Get the pivot points
            var thisPivot = rt.pivot;
            var canvasPivot = canvas.pivot;

            // Set up some arrays of coordinates in various reference systems
            var worldSpaces = new Vector3[transforms.Length];
            var canvasSpaces = new Vector3[transforms.Length];
            var points = new Vector2[transforms.Length];

            // First, convert the pivot to worldspace
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null) continue;
                worldSpaces[i] = transforms[i].TransformPoint(thisPivot);
            }

            // Then, convert to canvas space
            for (var i = 0; i < transforms.Length; i++) canvasSpaces[i] = canvas.InverseTransformPoint(worldSpaces[i]);

            // Calculate delta from the canvas pivot point
            for (var i = 0; i < transforms.Length; i++) points[i] = new Vector2(canvasSpaces[i].x, canvasSpaces[i].y);

            // And assign the converted points to the line renderer
            lr.Points = points;
            lr.RelativeSize = false;
            lr.drivenExternally = true;

            previousPositions = new Vector3[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null) continue;
                previousPositions[i] = transforms[i].position;
            }
        }
    }
}