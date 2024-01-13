using UnityEngine;

namespace PathFinding
{
    public class Node
    {
        private static readonly float DefaultSize = 1;
        private static readonly float EqualityPrecision = 0.01f;

        public readonly Vector3 Position;

        public readonly float Size;
        public readonly float SlopeAngle;

        // Neighbours
        public Node[] Neighbours;
        public Node Parent;

        public Node(Vector3 position, float slopeAngle, float? size)
        {
            Position = position;
            SlopeAngle = slopeAngle;
            Size = size ?? DefaultSize;
        }


        // Function = Cost + Heuristic
        public float F => G + H;
        public float G { get; set; }
        public float H { get; set; }

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