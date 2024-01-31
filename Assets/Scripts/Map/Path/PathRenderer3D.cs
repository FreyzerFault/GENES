using System.Collections.Generic;
using ExtensionMethods;
using UnityEngine;
#if UNITY_EDITOR
using MyBox;
#endif

namespace Map.Path
{
    public class PathRenderer3D : MonoBehaviour, IPathRenderer<PathObject>
    {
        // RENDERER
        [SerializeField] protected PathObject pathObjPrefab;
        [SerializeField] protected List<PathObject> pathObjects = new();

        // PATH
        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField] private float lineThickness = 1f;


        public float LineThickness
        {
            get => lineThickness;
            set
            {
                lineThickness = value;
                pathObjects.ForEach(line => { line.LineThickness = value; });
            }
        }


        // ============================= INITIALIZATION =============================
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

        public int PathCount => pathObjects.Count;

        public bool IsEmpty => PathCount == 0;


        // ============================= MODIFY PATH RENDERERS =============================

        public void AddPath(PathFinding.Path path, int index = -1)
        {
            if (index == -1) index = pathObjects.Count;

            var pathObj = Instantiate(pathObjPrefab, transform);
            pathObjects.Insert(index, pathObj);

            // Initilize properties
            pathObj.LineThickness = lineThickness;
            pathObj.heightOffset = heightOffset;
            UpdateColors();

            UpdateLine(path, index);
        }

        public void RemovePath(int index = -1)
        {
            if (index == -1) index = pathObjects.Count - 1;

            if (Application.isPlaying)
                Destroy(pathObjects[index].gameObject);
            else
                DestroyImmediate(pathObjects[index].gameObject);

            pathObjects.RemoveAt(index);

            // Si no es el ultimo, los colores deberian actualizarse para mantener la coherencia
            if (index < pathObjects.Count) UpdateColors();
        }

        public void ClearPaths()
        {
            foreach (var pathObject in pathObjects)
                if (Application.isPlaying)
                    Destroy(pathObject.gameObject);
                else
                    DestroyImmediate(pathObject.gameObject);
            pathObjects.Clear();
        }

        public void UpdateLine(PathFinding.Path path, int index)
        {
            pathObjects[index].Path = path;
        }

        public void UpdateAllLines(PathFinding.Path[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
                if (i >= pathObjects.Count) AddPath(paths[i]);
                else UpdateLine(paths[i], i);

            UpdateColors();
        }


        // Assign Colors progressively like a rainbow :D
        private void UpdateColors()
        {
            Color.yellow.GetRainBowColors(PathCount, 0.2f).ForEach(
                (color, i) => pathObjects[i].Color = color
            );
        }
    }
}