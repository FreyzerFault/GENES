using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map.Path
{
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        [SerializeField] private bool projectOnTerrain = true;
        [SerializeField] private float heightOffset = 0.5f;


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

            // HEIGHT OFFSET
            var offset = Vector3.up * heightOffset;
            lineSamples = lineSamples.Select(point => point += offset).ToList();

            return lineSamples.ToArray();
        }
    }
}