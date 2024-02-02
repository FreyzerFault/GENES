using UnityEngine;

namespace Map.Rendering
{
    public interface IPathRenderer<T> where T : Object
    {
        public int PathCount { get; }

        public bool IsEmpty { get; }

        public void AddPath(PathFinding.Path path, int index = -1);

        public void RemovePath(int index = -1);

        public void UpdateAllLines(PathFinding.Path[] paths);

        // Asigna un Path a un LineRenderer
        public void UpdateLine(PathFinding.Path path, int index);
        public void ClearPaths();
    }
}