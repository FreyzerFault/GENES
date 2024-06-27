using System.Collections;
using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.Geometry.Bounding_Box;
using TreesGeneration;
using UnityEngine;

// Testea semillas aleatorias automaticamente, con una especie de reproductor
// Paralo cuando veas una anomalia

namespace GENES.TreesGeneration.Tests
{
	[RequireComponent(typeof(OliveGroveGenerator))]
	public class GenOlivosTest : TestRunner
	{
		private OliveGroveGenerator _generator;
		private OliveGroveGenerator Generator => _generator ??= GetComponent<OliveGroveGenerator>();
		private BoundsComponent Bounds => Generator.BoundsComp;

		public int initialSeed;
		public int numFincas = 9;

		[SerializeField] private int mapSize = 200;
		public int MapSize
		{
			get => mapSize;
			set
			{
				mapSize = value;
				UpdateMapSize();
			}
		}

		protected override void Awake()
		{
			Generator.randSeed = initialSeed;
			Generator.numSeeds = numFincas;
			UpdateMapSize();
			FocusCamera();

			base.Awake();
		}

		protected override void InitializeTests() => AddTest(
			RunGenerator,
			new TestInfo("TestCoroutine", GeneratorSuccessCondition)
		);

		private IEnumerator RunGenerator()
		{
			Generator.Reset();
			if (iterations == 0) Generator.GenerateSeeds();
			else Generator.RandomizeSeeds();

			yield return Generator.RunCoroutine();
		}

		private bool GeneratorSuccessCondition() => Generator.Data.All(d => d.Olivos.Any());


		#region MAP SIZE

		private void UpdateMapSize()
		{
			Bounds.Size2D = mapSize * Vector2.one;
			FocusCamera();
		}

		#endregion


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
