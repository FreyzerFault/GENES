using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using PathFinding;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Map.Rendering
{
    public class PathRendererUI : MonoBehaviour, IPathRenderer<UILineRenderer>
    {
        // RENDERER
        [SerializeField] protected UILineRenderer linePrefab;
        [SerializeField] protected UILineRenderer illegalLinePrefab;
        [SerializeField] protected List<UILineRenderer> lineRenderers = new();

        [SerializeField] private float lineThickness = 1f;

        private MapRendererUI _mapRendererUI;
        private Terrain _terrain;

        private RectTransform MapRectTransform => _mapRendererUI.GetComponent<RectTransform>();

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
            _mapRendererUI = GetComponentInParent<MapRendererUI>();
        }

        private void Start()
        {
            PathGenerator.Instance.OnPathAdded += AddPath;
            PathGenerator.Instance.OnPathDeleted += RemovePath;
            PathGenerator.Instance.OnPathUpdated += UpdateLine;
            PathGenerator.Instance.OnAllPathsUpdated += UpdateAllLines;
            PathGenerator.Instance.OnPathsCleared += ClearPaths;
            UpdateAllLines(PathGenerator.Instance.paths.ToArray());
        }

        private void OnDestroy()
        {
            PathGenerator.Instance.OnPathAdded -= AddPath;
            PathGenerator.Instance.OnPathDeleted -= RemovePath;
            PathGenerator.Instance.OnPathUpdated -= UpdateLine;
            PathGenerator.Instance.OnAllPathsUpdated -= UpdateAllLines;
            PathGenerator.Instance.OnPathsCleared -= ClearPaths;
            ClearPaths();
        }


        public int PathCount => lineRenderers.Count;

        public bool IsEmpty => PathCount == 0;

        // ============================= MODIFY LIST =============================

        public void AddPath(Path path, int index = -1)
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
            foreach (var lineRenderer in lineRenderers)
                if (Application.isPlaying)
                    Destroy(lineRenderer.gameObject);
                else
                    DestroyImmediate(lineRenderer.gameObject);
            lineRenderers.Clear();
        }

        public void UpdateLine(Path path, int index = -1) =>
            lineRenderers[index].Points = PathToLine(path);

        public void UpdateAllLines(Path[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
                if (i >= lineRenderers.Count) AddPath(paths[i]);
                else UpdateLine(paths[i], i);
        }

        // Convierte el Path en las Coordenadas 2D que necesita el UILineRenderer
        private Vector2[] PathToLine(Path path)
        {
            if (path.NodeCount < 2 || path.IsIllegal) return Array.Empty<Vector2>();
            return path
                .GetPathNormalizedPoints(_terrain)
                .Select(MapRectTransform.NormalizedToLocalPoint)
                .ToArray();
        }
    }
}