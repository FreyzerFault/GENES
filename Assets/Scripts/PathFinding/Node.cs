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

        public Vector3 position;
        public float size;
        public float slopeAngle;
        public Vector2 direction;

        // Es legal hasta que se demuestre lo contrario
        private bool _legal = true;
        private Node _parent;

        // Neighbours
        [NonSerialized] public Node[] Neighbours;

        public Node(
            Vector3 position,
            float? slopeAngle = null,
            float? size = null,
            Vector2? direction = null
        )
        {
            this.position = position;
            this.slopeAngle = slopeAngle ?? Terrain.activeTerrain.GetSlopeAngle(position);
            this.size = size ?? DefaultSize;
            this.direction = direction ?? Vector2.zero;
        }

        public Node Parent
        {
            get => _parent;
            set
            {
                // Update Direction from Parent
                direction = Direction(value, this);
                _parent = value;
            }
        }

        // Function = Cost + Heuristic
        public float F => G + H;
        public float G { get; set; }
        public float H { get; set; }

        public bool Legal
        {
            get => _legal;
            set => _legal = value;
        }

        public Vector2 Pos2D => new(position.x, position.z);
        public float Height => position.y;

        public static Vector2 Direction(Node from, Node to) => (to.Pos2D - from.Pos2D).normalized;

        public override int GetHashCode() => Pos2D.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is not Node node) return false;

            return Mathf.Abs(position.x - node.position.x) < EqualityPrecision
                   && Mathf.Abs(position.z - node.position.z) < EqualityPrecision;
        }

        public float Distance2D(Node node) => Mathf.Sqrt(Distance2DnoSqrt(node));

        public float Distance2DnoSqrt(Node node)
        {
            var xDelta = position.x - node.position.x;
            var zDelta = position.z - node.position.z;

            return xDelta * xDelta + zDelta * zDelta;
        }

        public bool Collision(Node node) => Distance2DnoSqrt(node) < size;
    }
}