using System.Linq;
using DavidUtils.Spawning;
using UnityEngine;

namespace TreesGeneration
{
	public class SpawnerTrees : SpawnerBoxInTerrain
	{
		[SerializeField] private float minDistanceBetweenTrees = 10;
		[SerializeField] private int maxTries = 10;

		public override Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
		{
			Vector3 pos = GetRandomPosInTerrain();
			Vector3[] otherItemPositions = Parent.GetComponentsInChildren<Spawneable>()
				.Select(obj => obj.transform.position)
				.ToArray();

			var tries = 0;

			while (tries < maxTries && otherItemPositions.Any(
				       other => Vector3.Distance(pos, other) < minDistanceBetweenTrees
			       ))
			{
				pos = GetRandomPosInTerrain();
				tries++;
			}

			return tries == maxTries ? null : Spawn(pos, spawnWithRandomRotation ? GetRandomRotation() : null);
		}
	}
}
