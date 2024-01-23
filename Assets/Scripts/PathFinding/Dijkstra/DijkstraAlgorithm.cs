using System;
using System.Collections.Generic;
using PathFinding;
using UnityEngine;

public class DijkstraAlgorithm : PathFindingAlgorithm
{
    private static DijkstraAlgorithm _instance;
    public static DijkstraAlgorithm Instance => _instance ??= new DijkstraAlgorithm();

    public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig,
        out List<Node> exploredNodes, out List<Node> openNodes)
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

    protected override bool IsLegal(Node node, PathFindingConfigSO paramsConfig)
    {
        throw new NotImplementedException();
    }

    protected override Node[] CreateNeighbours(Node node, Terrain terrain, Node[] nodesAlreadyFound)
    {
        throw new NotImplementedException();
    }

    protected override bool OutOfBounds(Vector2 pos, Terrain terrain)
    {
        throw new NotImplementedException();
    }

    protected override bool IllegalPosition(Vector2 pos, Terrain terrain)
    {
        throw new NotImplementedException();
    }
}