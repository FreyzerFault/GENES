using System.Collections.Generic;
using PathFinding;
using UnityEngine;

namespace Map
{
    public class DirectPathGenerator : PathGenerator
    {
        protected override Path BuildPath(
            Vector3 start, Vector3 end, Vector2? initialDirection = null, Vector2? endDirection = null
        )
        {
            var startNode = new Node(start, direction: initialDirection);
            var endNode = new Node(end, direction: endDirection);
            endNode.Parent = startNode;
            return new Path(startNode, endNode);
        }

        protected override List<Path> BuildPath(Vector3[] checkPoints, Vector2[] initialDirections = null)
        {
            var pathsBuilt = new List<Path>();
            var haveDirections = initialDirections is { Length: > 0 };

            for (var i = 1; i < checkPoints.Length; i++)
            {
                var start = checkPoints[i - 1];
                var end = checkPoints[i];

                var startDirection = haveDirections && initialDirections.Length > i - 1
                    ? initialDirections[i - 1]
                    : Vector2.zero;
                var endDirection = haveDirections && initialDirections.Length > i
                    ? initialDirections[i]
                    : Vector2.zero;

                pathsBuilt.Add(BuildPath(start, end, startDirection, endDirection));
            }

            return pathsBuilt;
        }
    }
}