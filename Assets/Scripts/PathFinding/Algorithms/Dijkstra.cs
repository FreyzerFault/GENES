using System;
using PathFinding.Settings;
using UnityEngine;

namespace PathFinding.Algorithms
{
    public class Dijkstra : PathFindingAlgorithm
    {
        private static Dijkstra _instance;
        public static Dijkstra Instance => _instance ??= new Dijkstra();

        public override Path FindPath(Node start, Node end, Terrain terrain, Bounds bounds, PathFindingSettings settings) =>
            throw new NotImplementedException();

        protected virtual float CalculateCost(Node a, Node b, PathFindingSettings pathFindingSettings) =>
            throw new NotImplementedException();
    }
}