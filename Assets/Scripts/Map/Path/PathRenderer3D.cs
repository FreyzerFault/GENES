using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using MyBox;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
#if UNITY_EDITOR
#endif

namespace Map.Path
{
    public class PathRenderer3D : MonoBehaviour, IPathRenderer<LineRenderer>
    {
        // RENDERER
        [SerializeField] protected LineRenderer linePrefab;
        [SerializeField] protected List<LineRenderer> lineRenderers = new();

        // PATH
        [SerializeField] protected List<PathFinding.Path> paths = new();
        [SerializeField] private bool projectOnTerrain;
        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField] private float lineThickness = 1f;

        private Terrain _terrain;

        // ============================= INITIALIZATION =============================
        private void Awake()
        {
            _terrain = Terrain.activeTerrain;
        }

        private void Start()
        {
            if (!IsEmpty) UpdateAllLines();
        }


        public List<PathFinding.Path> Paths
        {
            get => paths;
            set
            {
                paths = value;
                UpdateAllLines();
            }
        }

        public PathFinding.Path Path
        {
            get => PathCount == 0 ? PathFinding.Path.EmptyPath : paths[0];
            set
            {
                if (PathCount == 0)
                {
                    AddPath(value);
                }
                else
                {
                    paths[0] = value;
                    UpdateLine(0);
                }
            }
        }

        public int PathCount => Paths.Count;

        public bool IsEmpty => PathCount == 0 || Path.IsEmpty;

        // ============================= MODIFY LIST =============================

        public void AddPath(PathFinding.Path path, int index = -1)
        {
            if (index == -1) index = lineRenderers.Count;

            var lineRenderer = Instantiate(linePrefab, transform);
            lineRenderers.Insert(index, lineRenderer);
            paths.Insert(index, path);

            UpdateColors();

            lineRenderer.widthMultiplier = lineThickness;

            // Asigna el Path al LineRenderer
            UpdateLine(index);
        }

        public void RemovePath(int index = -1)
        {
            if (index == -1) index = lineRenderers.Count - 1;

            if (Application.isPlaying)
                Destroy(lineRenderers[index].gameObject);
            else
                DestroyImmediate(lineRenderers[index].gameObject);

            lineRenderers.RemoveAt(index);
            paths.RemoveAt(index);

            if (index < lineRenderers.Count) UpdateColors();
        }

        // ============================= UPDATE LINE RENDERERS =============================

        public void UpdateAllLines()
        {
            for (var i = 0; i < PathCount; i++)
                if (i >= lineRenderers.Count) AddPath(paths[i]);
                else UpdateLine(i);

            UpdateColors();
        }


        // Asigna un Path a un LineRenderer
        public void UpdateLine(int index)
        {
            var path = Paths[index];
            var lineRenderer = lineRenderers[index];

            if (path.NodeCount < 2)
            {
                lineRenderer.positionCount = 0;
                lineRenderer.SetPositions(Array.Empty<Vector3>());
                return;
            }

            _terrain ??= Terrain.activeTerrain;

            var points = path.GetPathWorldPoints();

            // Proyectar en el Terreno
            if (projectOnTerrain) points = _terrain.ProjectPathToTerrain(points);

            // HEIGHT OFFSET
            var offset = Vector3.up * heightOffset;
            points = points.Select(point => point + offset).ToArray();

            // Update Line Renderer
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }


#if UNITY_EDITOR
        [ButtonMethod]
#endif
        public void ClearPaths()
        {
            for (var i = 0; i < PathCount; i++) RemovePath();
        }

        private void UpdateColors()
        {
            Color.yellow.GetRainBowColors(PathCount).ForEach(
                (color, i) => lineRenderers[i].startColor = lineRenderers[i].endColor = color
            );
        }
    }
}