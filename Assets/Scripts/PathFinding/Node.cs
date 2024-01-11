using UnityEngine;

namespace PathFinding
{
    public class Node
    {
        private static readonly Vector2 DefaultSize = Vector2.one;

        public readonly Vector3 Position;

        public readonly Vector2 Size;
        public readonly float SlopeAngle;

        // Neighbours
        public Node[] Neighbours;
        public Node Parent;

        public Node(Vector3 position, float slopeAngle, Vector2? size)
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
    }
}