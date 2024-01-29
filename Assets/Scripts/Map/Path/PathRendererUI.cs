using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Map.Path
{
    public class PathRendererUI : MonoBehaviour, IPathRenderer<UILineRenderer>
    {
        // RENDERER
        [SerializeField] protected UILineRenderer linePrefab;
        [SerializeField] protected List<UILineRenderer> lineRenderers = new();

        [SerializeField] private float lineThickness = 1f;

        private MapUIRenderer _mapUIRenderer;
        private Terrain _terrain;

        private RectTransform MapRectTransform => _mapUIRenderer.GetComponent<RectTransform>();

        public float LineThickness
        {
            get => lineThickness;
            set
            {
                lineThickness = value;
                lineRenderers.ForEach(line => { line.LineThickness = value; });
            }
        }

        // ============================= INITIALIZATION =============================
        private void Awake()
        {
            _terrain = Terrain.activeTerrain;
            _mapUIRenderer = GetComponentInParent<MapUIRenderer>();
        }

        private void Start()
        {
            PathGenerator.Instance.OnPathAdded += AddPath;
            PathGenerator.Instance.OnPathDeleted += RemovePath;
            PathGenerator.Instance.OnPathUpdated += UpdateLine;
            PathGenerator.Instance.OnPathsCleared += ClearPaths;
            UpdateAllLines(PathGenerator.Instance.paths.ToArray());
        }

        public int PathCount => lineRenderers.Count;

        public bool IsEmpty => PathCount == 0;

        // ============================= MODIFY LIST =============================

        public void AddPath(PathFinding.Path path, int index = -1)
        {
            if (index == -1) index = lineRenderers.Count;

            var lineRenderer = Instantiate(linePrefab, transform);
            lineRenderers.Insert(index, lineRenderer);

            // Initilize properties
            lineRenderer.LineThickness = lineThickness;

            UpdateLine(path, index);
        }

        public void RemovePath(int index = -1)
        {
            if (index == -1) index = lineRenderers.Count - 1;

            if (Application.isPlaying)
                Destroy(lineRenderers[index].gameObject);
            else
                DestroyImmediate(lineRenderers[index].gameObject);

            lineRenderers.RemoveAt(index);
        }

        public void ClearPaths()
        {
            for (var i = 0; i < PathCount; i++) RemovePath(i);
        }

        public void UpdateLine(PathFinding.Path path, int index = -1)
        {
            lineRenderers[index].Points = PathToLine(path);
        }

        public void UpdateAllLines(PathFinding.Path[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
                if (i >= lineRenderers.Count) AddPath(paths[i]);
                else UpdateLine(paths[i], i);
        }

        // Convierte el Path en las Coordenadas 2D que necesita el UILineRenderer
        private Vector2[] PathToLine(PathFinding.Path path)
        {
            if (path.NodeCount < 2) return Array.Empty<Vector2>();
            return path
                .GetPathNormalizedPoints(_terrain)
                .Select(MapRectTransform.NormalizedToLocalPoint)
                .ToArray();
        }
    }
}