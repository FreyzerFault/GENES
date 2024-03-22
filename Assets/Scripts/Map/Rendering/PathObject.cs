using System;
using System.Globalization;
using System.Linq;
using ExtensionMethods;
using PathFinding;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Map.Rendering
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathObject : MonoBehaviour
    {
        public bool showExplored = true;
        public bool showOpened = true;
        public bool showValues;
        public float heightOffset = 0.5f;

        public Node[] exploredNodes = Array.Empty<Node>();
        public Node[] openNodes = Array.Empty<Node>();

        private LineRenderer _lineRenderer;

        private Path _path;

        private Terrain _terrain;

        public Path Path
        {
            get => _path;
            set
            {
                _path = value;
                UpdateLineRenderer();
            }
        }

        public float LineThickness
        {
            get => _lineRenderer.widthMultiplier;
            set => _lineRenderer.widthMultiplier = value;
        }

        public Color Color
        {
            get => _lineRenderer.startColor;
            set => _lineRenderer.startColor = _lineRenderer.endColor = value;
        }

        public Color StartColor
        {
            get => _lineRenderer.startColor;
            set => _lineRenderer.startColor = value;
        }

        public Color EndColor
        {
            get => _lineRenderer.endColor;
            set => _lineRenderer.endColor = value;
        }

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _terrain = Terrain.activeTerrain;
        }

        private void Start()
        {
            UpdateLineRenderer();
        }

        private void UpdateLineRenderer()
        {
            if (_path == null || _path.NodeCount < 2) return;

            // HEIGHT OFFSET
            var points = _path.GetPathWorldPoints();
            points = points.Select(point => point + Vector3.up * heightOffset).ToArray();

            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            var openedColor = Color.Lerp(Color, Color.white, 0.9f);
            if (showExplored && _path.ExploredNodes.Length > 0)
                _path.ExploredNodes.ToList().ForEach(node => DrawNodeGizmos(node, Color));
            if (showOpened && _path.OpenNodes.Length > 0)
                _path.OpenNodes.ToList().ForEach(node => DrawNodeGizmos(node, openedColor));
        }

        private void DrawNodeGizmos(Node node, Color color, bool wire = false)
        {
            var pos = node.position;
            pos.y += heightOffset;
            var normPos = _terrain.GetNormalizedPosition(pos);
            var normal = _terrain.terrainData.GetInterpolatedNormal(normPos.x, normPos.y);
            var tangentMid = Vector3.Cross(normal, Vector3.up);
            var tangentGradient = Vector3.Cross(normal, tangentMid);

            // Diferencia de Funcion
            Gizmos.color = color.Darken(0.5f);

            // Cubo
            var size = new Vector3(node.size / 3, 0.1f, node.size / 3);
            if (wire)
                Gizmos.DrawWireCube(pos, size);
            else
                Gizmos.DrawCube(pos, size);

            // PENDIENTE
            if (node.slopeAngle > 0)
            {
                // Normal
                Gizmos.color = Color.Lerp(Color.magenta, Color.red, node.slopeAngle / 30);
                DrawArrow(pos, normal, node.size / 2);

                // Gradiente
                Gizmos.color = Color.blue;
                DrawArrow(pos, tangentGradient);
            }

            // DIRECTION
            if (node.direction != Vector2.zero)
            {
                Gizmos.color = Color.yellow;
                DrawArrow(pos, new Vector3(node.direction.x, 0, node.direction.y), node.size / 2);
            }

            // Line to Parent
            if (node.Parent != null)
            {
                Gizmos.color = Color.Lerp(color, Color.white, 0.5f);
                Gizmos.DrawLine(pos, node.Parent.position + Vector3.up * heightOffset);
            }

            // [F,G,H] Labels
            if (showValues) DrawLabel(node, Vector3.left * node.size / 3 + Vector3.up * heightOffset);
        }

        private void DrawArrow(Vector3 pos, Vector3 direction, float size = 1)
        {
            var tangent = Vector3.Cross(direction, Vector3.up);
            var arrowVector = direction * size;
            Gizmos.DrawLineList(
                new[]
                {
                    pos,
                    pos + arrowVector,
                    pos + arrowVector,
                    pos + arrowVector - Quaternion.AngleAxis(30, tangent) * arrowVector * 0.4f,
                    pos + arrowVector,
                    pos + arrowVector - Quaternion.AngleAxis(-30, tangent) * arrowVector * 0.4f
                }
            );
        }

        private void DrawLabel(Node node, Vector3 positionOffset = default)
        {
            // STYLE
            var style = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var styleF = new GUIStyle(style) { normal = { textColor = Color.white } };
            var styleG = new GUIStyle(style) { normal = { textColor = Color.red } };
            var styleH = new GUIStyle(style) { normal = { textColor = Color.yellow } };

            // TEXT
            var labelTextF = Math.Round(node.F, 2).ToString(CultureInfo.InvariantCulture);
            var labelTextG = Math.Round(node.G, 2).ToString(CultureInfo.InvariantCulture);
            var labelTextH = Math.Round(node.H, 2).ToString(CultureInfo.InvariantCulture);

            // POSITION
            var posF = node.position + Vector3.forward * 0.2f + positionOffset;
            var posG = node.position + positionOffset;
            var posH = node.position - Vector3.forward * 0.2f + positionOffset;

            Handles.Label(posF, labelTextF, styleF);
            Handles.Label(posG, labelTextG, styleG);
            Handles.Label(posH, labelTextH, styleH);
        }

#endif
    }
}