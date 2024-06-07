using System;
using System.Globalization;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
	[Serializable]
	public class Node
	{
		private const float DefaultSize = 1;
		protected const float EqualityPrecision = 0.01f;

		public Vector3 position;
		public float size;
		public float slopeAngle;
		public Vector2 direction;

		// Es legal hasta que se demuestre lo contrario
		private bool _legal = true;
		private Node _parent;

		// Neighbours
		[NonSerialized] public Node[] neighbours;

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
			float xDelta = position.x - node.position.x;
			float zDelta = position.z - node.position.z;

			return xDelta * xDelta + zDelta * zDelta;
		}

		public bool Collision(Node node) => Distance2DnoSqrt(node) < size * size;

		#region DEBUG

#if UNITY_EDITOR
		public void OnGizmos(Color color, float heightOffset = 0, bool wire = false, bool showValues = false)
		{
			var terrain = Terrain.activeTerrain;
			Vector3 pos = position;
			pos.y += heightOffset;
			Vector2 normPos = terrain.GetNormalizedPosition(pos);
			Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normPos.x, normPos.y);
			Vector3 tangentMid = Vector3.Cross(normal, Vector3.up).normalized;
			Vector3 tangentGradient = Vector3.Cross(normal, tangentMid).normalized;

			// Diferencia de Funcion
			Color nodeColor = color.Darken(0.5f);

			// Quad del nodo
			var cubeSize = new Vector3(size / 3, 0.1f, size / 3);
			Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
			if (wire) GizmosExtensions.DrawQuadWire(Matrix4x4.TRS(pos, rotation, cubeSize), 5, nodeColor);
			else GizmosExtensions.DrawQuad(Matrix4x4.TRS(pos, rotation, cubeSize), nodeColor);

			// PENDIENTE
			if (slopeAngle > 0.1f)
			{
				Color tangentColor = Color.Lerp(Color.gray, Color.red, slopeAngle / 30 - 0.2f);
				GizmosExtensions.DrawArrowWire(pos, tangentGradient, Vector3.right, 1, 0.4f, 5, tangentColor);
			}

			// DIRECTION
			if (direction != Vector2.zero)
				GizmosExtensions.DrawArrow(
					pos,
					new Vector3(direction.x, 0, direction.y),
					Vector3.right,
					size / 2,
					0.4f,
					Color.yellow
				);

			// Line to Parent
			if (Parent != null)
			{
				Gizmos.color = Color.Lerp(color, Color.white, 0.5f);
				Gizmos.DrawLine(pos, Parent.position + Vector3.up * heightOffset);
			}

			// [F,G,H] Labels
			if (showValues) DrawLabel(Vector3.left * size / 3 + Vector3.up * heightOffset);
		}

		private void DrawLabel(Vector3 positionOffset = default)
		{
			// TEXT
			var labelTextF = Math.Round(F, 2).ToString(CultureInfo.InvariantCulture);
			var labelTextG = Math.Round(G, 2).ToString(CultureInfo.InvariantCulture);
			var labelTextH = Math.Round(H, 2).ToString(CultureInfo.InvariantCulture);

			// POSITION
			Vector3 posF = position + Vector3.forward * 0.2f + positionOffset;
			Vector3 posG = position + positionOffset;
			Vector3 posH = position - Vector3.forward * 0.2f + positionOffset;

			// COLOR
			Color colorF = Color.white;
			Color colorG = Color.red;
			Color colorH = Color.yellow;


			GizmosExtensions.DrawLabel(posF, labelTextF, colorF);
			GizmosExtensions.DrawLabel(posG, labelTextG, colorG);
			GizmosExtensions.DrawLabel(posH, labelTextH, colorH);
		}
#endif

		#endregion
	}
}
