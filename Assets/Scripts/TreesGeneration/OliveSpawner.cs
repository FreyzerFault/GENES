using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GENES.TreesGeneration
{
	[RequireComponent(typeof(RegionGenerator))]
	public class OliveSpawner : SpawnerBoxInTerrain
	{
		[SerializeField] private Spawneable[] olivoPrefabs = Array.Empty<Spawneable>();

		private RegionGenerator _generator;

		protected override void Awake()
		{
			base.Awake();
			_generator = GetComponent<RegionGenerator>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			_generator.OnRegionPopulated += HandleOnRegionPopulated;
			_generator.OnClear += Clear;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			_generator.OnRegionPopulated -= HandleOnRegionPopulated;
			_generator.OnClear -= Clear;
		}

		private void HandleOnRegionPopulated(RegionData data)
		{
			if (data is OliveRegionData oliveData) 
				Spawn2D(oliveData.treePositions.Select(t => AABB2D.NormalizedToBoundsSpace(t)));
		}

		protected override Spawneable InstantiateItem(Spawneable prefab = null) => 
			base.InstantiateItem(prefab ?? GetRandomModel());

		private Spawneable GetRandomModel()
		{
			if (olivoPrefabs.IsNullOrEmpty())
				Debug.LogError("No hay prefabs de olivos asignados, asignalos en el inspector", this);
			return olivoPrefabs?[Random.Range(0, olivoPrefabs.Length)];
		}
	}
}
