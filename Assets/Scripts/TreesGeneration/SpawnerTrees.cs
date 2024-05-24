using System.Linq;
using DavidUtils.Spawning;
using UnityEngine;

namespace TreesGeneration
{
	public class SpawnerTrees : SpawnerBoxInTerrain
	{
		[SerializeField] private float minDistanceBetweenTrees = 10;
		[SerializeField] private int maxTries = 10;

		private Vector3[] SpawnedPositions => SpawnedItems.Select(s => s.transform.position).ToArray();

		/// <summary>
		///     No permite spawnear si colisiona con otro objeto spawneado
		/// </summary>
		protected override Spawneable Spawn(Vector3 position = default, Quaternion rotation = default) =>
			!Collision(position) ? base.Spawn(position, rotation) : null;

		private bool Collision(Vector3 position) => SpawnedPositions.Any(
			other => Vector3.Distance(position, other) < minDistanceBetweenTrees
		);


		public override Spawneable SpawnRandom(bool setRandomRotation = true)
		{
			// Intenta spawnear en una posición que no esté cerca de otros objetos
			// Hasta que cumpla con la ditancia mínima o se supere el máximo de intentos
			Spawneable spawnedItem = null;
			Vector3 position = GetRandomPointInBounds();
			var tries = 0;
			while (tries < maxTries && Collision(position))
			{
				position = GetRandomPointInBounds();
				tries++;
			}

			return tries == maxTries ? null : base.Spawn(position, setRandomRotation ? RandomRotation : default);
		}
	}
}
