using System.Collections.Generic;
using UnityEngine;

namespace Map.Path
{
    public interface IPathRenderer<T> where T : Object
    {
        public List<PathFinding.Path> Paths { get; set; }

        public PathFinding.Path Path { get; set; }

        public int PathCount { get; }

        public bool IsEmpty { get; }

        public void AddPath(PathFinding.Path path, int index = -1);

        public void RemovePath(int index = -1);

        public void UpdateAllLines();

        // Asigna un Path a un LineRenderer
        public void UpdateLine(int index);
        public void ClearPaths();
    }
}