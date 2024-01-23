using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExtensionMethods;
using PathFinding;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Map.Path
{
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        [SerializeField] private bool projectOnTerrain = true;
        [SerializeField] private float heightOffset = 0.5f;

        public List<Node> exploredNodes = new();
        public List<Node> openNodes = new();

        public Color color;
        
        public bool showLabels = true;


        private PathFinding.Path _path = PathFinding.Path.EmptyPath;

        private Terrain _terrain;

        public PathFinding.Path Path
        {
            get => _path;
            set
            {
                _path = value;
                UpdateLine();
            }
        }

        private void Awake()
        {
            _terrain = Terrain.activeTerrain;
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            lineRenderer.startColor = lineRenderer.endColor = color;
        }

        private void OnDrawGizmosSelected()
        {
            exploredNodes.ForEach(node => DrawNodeGizmos(node, color, false));
            openNodes.ForEach(node => DrawNodeGizmos(node, Color.Lerp(color, Color.white, 0.5f), true));
        }

        private void DrawNodeGizmos(Node node, Color color, bool wire = false)
        {
            var pos = node.Position;
            pos.y += heightOffset;
            var normPos = _terrain.terrainData.GetNormalizedPosition(pos);
            var normal = _terrain.terrainData.GetInterpolatedNormal(normPos.x, normPos.y);
            var tangentMid = Vector3.Cross(normal, Vector3.up);
            var tangentGradient = Vector3.Cross(normal, tangentMid);
            
            // Diferencia de Funcion
            if (node.parent != null)
            {
                var functionDiff = node.parent.F - node.F;
                Gizmos.color = Color.Lerp(color.Darken(0.8f), color, functionDiff / 2f + 0.5f);
            }
            else
            {
                Gizmos.color = color;
            }

            // Cubo
            Vector3 size = new Vector3(node.Size / 3, 0.1f, node.Size / 3);
            if (wire)
                Gizmos.DrawWireCube(pos, size);
            else
                Gizmos.DrawCube(pos, size);


            // PENDIENTE
            if (node.SlopeAngle > 0)
            {
                // Normal
                Gizmos.color = Color.Lerp(Color.magenta, Color.red, node.SlopeAngle / 30);
                DrawArrow(pos, normal, node.Size / 2);
                
                // Gradiente
                Gizmos.color = Color.blue;
                DrawArrow(pos, tangentGradient);
            }


            // Line to Parent
            if (node.parent != null)
            {
                Gizmos.color = Color.Lerp(color, Color.white, 0.5f);
                Gizmos.DrawLine(pos, node.parent.Position + Vector3.up * heightOffset);
            }

            // [F,G,H] Labels
            if (showLabels)
                DrawLabel(node, Vector3.left * node.Size / 3 + Vector3.up * heightOffset);
        }

        private void DrawArrow(Vector3 pos, Vector3 direction, float size = 1)
        {
            var tangent = Vector3.Cross(direction, Vector3.up);
            Vector3 arrowVector = direction * size;
            Gizmos.DrawLineList(new[]
            {
                pos,
                pos + arrowVector,
                pos + arrowVector,
                pos + arrowVector - Quaternion.AngleAxis(30, tangent) * arrowVector * 0.4f,
                pos + arrowVector,
                pos + arrowVector - Quaternion.AngleAxis(-30, tangent) * arrowVector * 0.4f
            });
        }

        private void DrawLabel(Node node, Vector3 positionOffset = default)
        {
            // STYLE
            GUIStyle style = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUIStyle styleF = new GUIStyle(style) { normal = { textColor = Color.white } };
            GUIStyle styleG = new GUIStyle(style) { normal = { textColor = Color.red } };
            GUIStyle styleH = new GUIStyle(style) { normal = { textColor = Color.yellow } };

            // TEXT
            string labelTextF = Math.Round(node.F, 2).ToString(CultureInfo.InvariantCulture);
            string labelTextG = Math.Round(node.G, 2).ToString(CultureInfo.InvariantCulture);
            string labelTextH = Math.Round(node.H, 2).ToString(CultureInfo.InvariantCulture);
            
            // POSITION
            Vector3 posF = node.Position + Vector3.forward * 0.2f + positionOffset;
            Vector3 posG = node.Position + positionOffset;
            Vector3 posH = node.Position - Vector3.forward * 0.2f  + positionOffset;
            
            Handles.Label(posF, labelTextF, styleF);
            Handles.Label(posG, labelTextG, styleG);
            Handles.Label(posH, labelTextH, styleH);
        }

        private void UpdateLine()
        {
            _terrain ??= Terrain.activeTerrain;

            var points = _path.GetPathWorldPoints();

            if (points.Length < 2)
            {
                ClearLine();
                return;
            }

            // Proyectar en el Terreno
            if (projectOnTerrain) points = ProjectPathToTerrain(points);
            
            // HEIGHT OFFSET
            var offset = Vector3.up * heightOffset;
            points = points.Select(point => point += offset).ToArray();

            // Update Line Renderer
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }

        public void ClearLine()
        {
            lineRenderer.positionCount = 0;
            lineRenderer.SetPositions(Array.Empty<Vector3>());
        }

        // ================== TERRAIN PROJECTION ==================
        private Vector3[] ProjectPathToTerrain(Vector3[] path)
        {
            if (path.Length == 0) return Array.Empty<Vector3>();
            var finalPath = Array.Empty<Vector3>();
            for (var i = 1; i < path.Length; i++)
                finalPath = finalPath.Concat(ProjectSegmentToTerrain(path[i - 1], path[i]).SkipLast(1)).ToArray();
            finalPath = finalPath.Append(path[^1]).ToArray();

            return finalPath;
        }

        // Upsample un segmento proyectandolo en el terreno
        private Vector3[] ProjectSegmentToTerrain(Vector3 a, Vector3 b)
        {
            var distance = Vector3.Distance(a, b);
            var sampleLength = _terrain.terrainData.heightmapScale.x;

            var lineSamples = new List<Vector3>();

            // Si el segmento es más corto, no hace falta samplearlo
            if (sampleLength > distance)
            {
                lineSamples = new List<Vector3>(new[] { a, b });
            }
            else
            {
                var numSamples = Mathf.FloorToInt(distance / sampleLength);
                for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
                {
                    // Por cada sample, calcular su altura mapeada al terreno
                    var samplePos = a + (b - a) * ((float)sampleIndex / numSamples);
                    samplePos.y = _terrain.SampleHeight(samplePos);

                    lineSamples.Add(samplePos);
                }
            }

            return lineSamples.ToArray();
        }
    }
}