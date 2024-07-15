using System.Collections;
using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;

// Testea semillas aleatorias automaticamente, con una especie de reproductor
// Paralo cuando veas una anomalia

namespace GENES.TreesGeneration.Tests
{
	[RequireComponent(typeof(OliveGroveGenerator))]
	public class GenOlivosOnTerrainTest : TestRunner
	{
		private RegionGenerator _generator;
		private RegionGenerator Generator => _generator ??= GetComponent<RegionGenerator>();
		private BoundsComponent Bounds => Generator.BoundsComp;

		public int initialSeed;
		public int numFincas = 9;

		private Terrain _terrain;
		public Vector2 MapSize
		{
			get => _terrain.terrainData.size.ToV2xz();
			set => _terrain.terrainData.size = value.ToV3xz().WithY(_terrain.terrainData.size.y);
		}

		protected override void Awake()
		{
			_terrain = Terrain.activeTerrain;

			if (_terrain == null)
			{
				Debug.LogError("Terrain is not found");
				base.Awake();
				return;
			}

			base.Awake();
		}

		protected override void Start()
		{
			Bounds.AdjustToTerrain(_terrain);
			
			FocusCamera();

			Generator.randSeed = initialSeed;
			Generator.NumSeeds = numFincas;
			base.Start();
		}


		protected override void InitializeTests()
		{
			AddTest(
				RunGenerator,
				new TestInfo("TestCoroutine", GeneratorSuccessCondition)
			);
		}

		private IEnumerator RunGenerator()
		{
			Generator.Reset();
			if (iterations == 0) Generator.GenerateSeeds();
			else Generator.RandomizeSeeds();

			yield return Generator.RunCoroutine();
		}

		private bool GeneratorSuccessCondition() => 
			Generator.Data.NotNullOrEmpty()
            && Generator.Data.All(data =>
			{
				if (data is OliveRegionData oliveData)
					return oliveData.Olivos.Any();

				// TODO otros tipos de datos
				return true;
			});


		#region CAMERA

		private void FocusCamera()
		{
			Camera cam = Camera.main;
			if (cam == null) return;

			cam.orthographicSize = Mathf.Max(Bounds.Size2D.x, Bounds.Size2D.y) * 0.5f;
		}

		#endregion
	}
}
