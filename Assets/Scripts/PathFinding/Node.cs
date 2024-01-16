using System;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    [Serializable]
    public class Node
    {
        private static readonly float DefaultSize = 1;
        private static readonly float EqualityPrecision = 0.01f;
        public Node parent;

        public readonly Vector3 Position;

        public readonly float Size;
        public readonly float SlopeAngle;

        // Neighbours
        [NonSerialized] public Node[] Neighbours;


        public Node(Vector3 position, float? slopeAngle = null, float? size = null)
        {
            Position = position;
            SlopeAngle = slopeAngle ?? Terrain.activeTerrain.GetSlopeAngle(position);
            Size = size ?? DefaultSize;
        }


        // Function = Cost + Heuristic
        public float F => G + H;
        public float G { get; set; }
        public float H { get; set; }
        public bool Legal { get; set; }

        public Vector2 Pos2D => new(Position.x, Position.z);
        public float Height => Position.y;

        public override int GetHashCode()
        {
            return Pos2D.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Node node) return false;

            return Vector2.Distance(node.Pos2D, Pos2D) < EqualityPrecision;
        }
    }
}