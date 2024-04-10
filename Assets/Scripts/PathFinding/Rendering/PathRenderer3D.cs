using System.Collections.Generic;
using DavidUtils.ExtensionMethods;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace PathFinding.Rendering
{
    public class PathRenderer3D : MonoBehaviour, IPathRenderer<PathObject>
    {
        [SerializeField] private PathGenerator pathFindingGenerator;
        
        [SerializeField] protected PathObject pathObjPrefab;
        [SerializeField] protected List<PathObject> pathObjects = new();
        public int PathCount => pathObjects.Count;
        public bool IsEmpty => PathCount == 0;

        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField] private Color color = Color.yellow;

        [SerializeField] private float lineThickness = 1f;
        public float LineThickness
        {
            get => lineThickness;
            set
            {
                lineThickness = value;
                pathObjects.ForEach(line => line.LineThickness = value);
            }
        }

        private void Awake() => pathFindingGenerator = GetComponent<PathGenerator>();

        private void Start()
        {
            pathFindingGenerator.OnPathAdded += AddPath;
            pathFindingGenerator.OnPathDeleted += RemovePath;
            pathFindingGenerator.OnPathUpdated += SetPath;
            pathFindingGenerator.OnAllPathsUpdated += UpdateAllLines;
            pathFindingGenerator.OnPathsCleared += ClearPaths;
            
            UpdateAllLines(pathFindingGenerator.Paths);
        }

        private void OnDestroy()
        {
            pathFindingGenerator.OnPathAdded -= AddPath;
            pathFindingGenerator.OnPathDeleted -= RemovePath;
            pathFindingGenerator.OnPathUpdated -= SetPath;
            pathFindingGenerator.OnAllPathsUpdated -= UpdateAllLines;
            pathFindingGenerator.OnPathsCleared -= ClearPaths;
            
            ClearPaths();
        }
        
        public void UpdateAllLines(Path[] paths)
        {
            for ( var i = 0; i < paths.Length; i++)
                if (i >= pathObjects.Count) AddPath(paths[i]);
                else SetPath(paths[i], i);

            UpdateColors();
        }

        #region CRUD

        public void AddPath(Path path, int index = -1)
        {
            if (index == -1) index = pathObjects.Count;

            PathObject pathObj = Instantiate(pathObjPrefab, transform);
            pathObjects.Insert(index, pathObj);

            // Initilize properties
            pathObj.LineThickness = lineThickness;
            pathObj.heightOffset = heightOffset;
            UpdateColors();

            SetPath(path, index);
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
            foreach (PathObject pathObject in pathObjects)
                if (Application.isPlaying)
                    Destroy(pathObject.gameObject);
                else
                    DestroyImmediate(pathObject.gameObject);
            pathObjects.Clear();
        }
        
        public void SetPath(Path path, int index) => pathObjects[index].Path = path;

        #endregion
        
        // Assign Colors progressively like a rainbow :D
        private void UpdateColors()
        {
            var rainbowColors = color.GetRainBowColors(PathCount, 0.2f);
            for (var i = 0; i < rainbowColors.Length; i++) pathObjects[i].Color = rainbowColors[i];
        }
    }
}