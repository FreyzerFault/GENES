using PathFinding.Settings;
using UnityEngine;

namespace PathFinding.Algorithms
{
	// ALGORTIMO A*
	public class AstarDirectional : Astar
	{
		private static AstarDirectional _instance;
		public new static AstarDirectional Instance => _instance ??= new AstarDirectional();


		// Añadimos al Coste el coste de giro
		protected override float CalculateCost(Node a, Node b, PathFindingSettings pathFindingSettings)
		{
			// Coste de GIRO = Ángulo entre la dirección para ir a b y la dirección con la que viene del parent de a
			Vector2 dirToB = Node.Direction(a, b);
			float angle = Vector2.Angle(a.direction, dirToB);
			float turn = Mathf.Pow(angle * Mathf.Deg2Rad, 2);
			float turnCost = turn * pathFindingSettings.aStarDirectionalParameters.TurnCost;

			return base.CalculateCost(a, b, pathFindingSettings) + turnCost;
		}

		// Misma Heuristica
	}
}
