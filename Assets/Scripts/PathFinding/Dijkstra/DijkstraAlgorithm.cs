using System;
using PathFinding;
using UnityEngine;

public class DijkstraAlgorithm : PathFindingAlgorithm
{
    private static DijkstraAlgorithm _instance;
    public static DijkstraAlgorithm Instance => _instance ??= new DijkstraAlgorithm();

    public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig)
    {
        throw new NotImplementedException();
    }

    protected override float CalculateCost(Node a, Node b, PathFindingConfigSO paramsConfig)
    {
        throw new NotImplementedException();
    }

    protected override float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig)
    {
        throw new NotImplementedException();
    }
}