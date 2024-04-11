using System;
using System.Collections.Generic;
using System.Linq;
using PathFinding.Settings;
using UnityEngine;

namespace PathFinding.Algorithms
{
	// ALGORTIMO A*
	public class Astar : DijkstraAlg
	{
		private static Astar _instance;
		public new static Astar Instance => _instance ??= new Astar();

		public override Path FindPath(
			Node start, Node end, Terrain terrain, Bounds bounds, PathFindingSettings settings
		)
		{
			if (start.Collision(end)) return Path.EmptyPath;

			// Si el Path de Cache tiene mismo inicio y fin => Devolverlo
			if (settings.useCache && IsCached(start, end, out Path pathCached)) return pathCached;

			if (!IsLegal(start, settings, bounds) || !IsLegal(end, settings, bounds)) return new Path(start, end);

			var iterations = 0;

			// Nodes checked && Nodes to check
			var allNodes = new HashSet<Node> { start };
			var openNodes = new List<Node> { start };
			var exploredNodes = new List<Node>();

			// Main loop
			while (openNodes.Count > 0)
			{
				iterations++;

				// Select BEST Node
				Node currentNode = GetBetterNode(openNodes);

				// Marcar como explorado
				openNodes.Remove(currentNode);
				exploredNodes.Add(currentNode);

				// Si cumple la condición objetivo => terminar algoritmo
				// End no tiene por qué ser un nodo que cuadre en la malla
				// Por lo que el último Nodo será el que se acerque a End colisionando con él
				bool endReached = currentNode.Collision(end);
				if (iterations >= settings.maxIterations || endReached)
				{
					// Como el Nodo actual esta demasiado cerca, lo ignoramos y conectamos el anterior a END
					Node lastNode = currentNode.Parent;
					end.G = currentNode.G + CalculateCost(lastNode, end, settings);
					end.H = 0;
					end.Parent = lastNode;

					var path = new Path(start, end);

					path.ExploredNodes = exploredNodes.ToArray();
					path.OpenNodes = openNodes.ToArray();

					// STORE in CACHE
					if (settings.useCache) StoreInCache(start, end, path);

					return path;
				}

				// Crear vecinos si es la 1º vez que se exploran
				if (currentNode.neighbours == null || currentNode.neighbours.Length == 0)
					currentNode.neighbours = CreateNeighbours(currentNode, settings, terrain, bounds, allNodes, false);

				// Explorar vecinos
				foreach (Node neighbour in currentNode.neighbours)
				{
					if (!neighbour.Legal) continue;

					bool explored = exploredNodes.Contains(neighbour);
					bool opened = openNodes.Contains(neighbour);

					// Ya explorado
					if (explored) continue;

					// Calcular coste
					float newCost = currentNode.G + CalculateCost(currentNode, neighbour, settings);

					if (newCost >= neighbour.G && opened) continue;

					// Si no está en la lista de exploración o su coste MEJORA, actualizar

					// Actualizar coste y Heurística
					neighbour.G = newCost;
					neighbour.H = CalculateHeuristic(neighbour, end, settings);

					// Conectar nodos
					neighbour.Parent = currentNode;

					if (!opened) openNodes.Add(neighbour);
				}
			}

			return Path.EmptyPath;
		}

		// ==================== COSTE Y HEURÍSTICA ====================
		protected override float CalculateCost(Node a, Node b, PathFindingSettings pathFindingSettings)
		{
			float distanceCost = a.Distance2D(b) * pathFindingSettings.Parameters.GetValue(ParamType.DistanceCost);
			float heightCost = Math.Abs(a.position.y - b.position.y)
			                   * pathFindingSettings.Parameters.GetValue(ParamType.HeightCost);

			return distanceCost + heightCost;
		}

		protected virtual float CalculateHeuristic(Node a, Node b, PathFindingSettings pathFindingSettings)
		{
			float distHeuristic =
				a.Distance2D(b) * pathFindingSettings.Parameters.GetValue(ParamType.DistanceHeuristic);
			float heightHeuristic =
				Mathf.Abs(a.position.y - b.position.y)
				* pathFindingSettings.Parameters.GetValue(ParamType.HeightHeuristic);

			float slopeHeuristic = a.slopeAngle * pathFindingSettings.Parameters.GetValue(ParamType.SlopeHeuristic);

			return distHeuristic + heightHeuristic + slopeHeuristic;
		}

		// Minimiza la Funcion objetivo
		// Si hay + de 1 Nodo con mismma F => Devuelve el que tenga menor H
		private static Node GetBetterNode(List<Node> nodes)
		{
			// Get Min F Nodes
			float minF = nodes.Min(node => node.F);
			List<Node> minFunctionNodes = nodes.Where(node => node.F - minF < float.Epsilon).ToList();

			if (minFunctionNodes.Count == 1) return minFunctionNodes[0];

			// Get Min H Nodes
			float minH = minFunctionNodes.Min(node => node.H);
			minFunctionNodes = minFunctionNodes.Where(node => node.H - minH < float.Epsilon).ToList();

			return minFunctionNodes[0];
		}
	}
}
