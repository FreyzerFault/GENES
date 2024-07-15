using System;
using System.Linq;
using UnityEngine;

namespace GENES.PathFinding.Rendering
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathObject : MonoBehaviour
    {
        public bool showExplored = true;
        public bool showOpened = true;
        public bool showValues;
        public float heightOffset = 0.5f;
        
        private void Awake() => _lineRenderer = GetComponent<LineRenderer>();

        private void Start() => UpdateLineRenderer();

        #region LINE RENDERER

        private LineRenderer _lineRenderer;

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
        private void UpdateLineRenderer()
        {
            if (_path == null || _path.NodeCount < 2) return;

            // HEIGHT OFFSET
            var points = _path.GetPathWorldPoints();
            points = points.Select(point => point + Vector3.up * heightOffset).ToArray();

            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
        }

        #endregion

        #region PATH
        
        private Path _path;
        private Node[] ExploredNodes => _path?.ExploredNodes ?? Array.Empty<Node>();
        private Node[] OpenNodes => _path?.OpenNodes ?? Array.Empty<Node>();
        
        public Path Path
        {
            get => _path;
            set
            {
                _path = value;
                UpdateLineRenderer();
            }
        }

        #endregion

        #region DEBUG

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Color exploredColor = Color.blue;
            Color openedColor = Color.white;
            if (showExplored && ExploredNodes.Length > 0)
                ExploredNodes.ToList().ForEach(node => node.OnGizmos(exploredColor, heightOffset, true, showValues));
            if (showOpened && OpenNodes.Length > 0)
                OpenNodes.ToList().ForEach(node => node.OnGizmos(openedColor, heightOffset, true, showValues));
        }

        #endif
        #endregion
    }
}
