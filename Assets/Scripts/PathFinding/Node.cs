using System;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    [Serializable]
    public class Node
    {
        private static readonly float DefaultSize = 1;
        protected static readonly float EqualityPrecision = 0.01f;

        public Vector3 Position;

        public float Size;
        public float SlopeAngle;

        // Direccion desde el parent al nodo
        public Vector2 direction;

        // Neighbours
        [NonSerialized] public Node[] Neighbours;
        private Node parent;


        public Node(Vector3 position, float? slopeAngle = null, float? size = null, Vector2? direction = null)
        {
            Position = position;
            SlopeAngle = slopeAngle ?? Terrain.activeTerrain.GetSlopeAngle(position);
            Size = size ?? DefaultSize;
            this.direction = direction ?? Vector2.zero;
        }

        public Node Parent
        {
            get => parent;
            set
            {
                // Update Direction from Parent
                direction = Direction(value, this);
                parent = value;
            }
        }


        // Function = Cost + Heuristic
        public float F => G + H;
        public float G { get; set; }
        public float H { get; set; }
        public bool Legal { get; set; }

        public Vector2 Pos2D => new(Position.x, Position.z);
        public float Height => Position.y;


        public static Vector2 Direction(Node from, Node to)
        {
            return (to.Pos2D - from.Pos2D).normalized;
        }

        public override int GetHashCode()
        {
            return Pos2D.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Node node) return false;

            return Mathf.Abs(Position.x - node.Position.x) < EqualityPrecision &&
                   Mathf.Abs(Position.z - node.Position.z) < EqualityPrecision;
        }

        public float Distance2D(Node node)
        {
            return Mathf.Sqrt(Distance2DnoSqrt(node));
        }

        public float Distance2DnoSqrt(Node node)
        {
            var xDelta = Position.x - node.Position.x;
            var zDelta = Position.z - node.Position.z;

            return xDelta * xDelta + zDelta * zDelta;
        }

        public bool Collision(Node node)
        {
            return Distance2DnoSqrt(node) < Size * Size;
        }
    }
}