using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using DavidUtils.Rendering;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace TreesGeneration
{
	public class OliveGroveGenerator : VoronoiGenerator
	{
		private int NumFincas => numSeeds;

		[Space]
		[Header("OLIVAS")]

		// Parametros para el layout de una finca
		[Range(0.1f, 10)]
		[SerializeField] private float minSeparation = 5;

		[Range(0.1f, 10)]
		[SerializeField] private float minDistToBoundary = 5;

		[Range(0.1f, 30)]
		[SerializeField] private float boundaryOffset = 10;

		public Dictionary<Polygon, Vector2[]> fincasDictionary = new();
		public Vector2[] OlivePositions => fincasDictionary?.SelectMany(pair => pair.Value).ToArray();
		public Vector2[] OlivePositionsByRegion(Polygon region) => fincasDictionary[region];


		public Action<Vector2[]> OnEndedGeneration;
		public Action<Vector2[]> OnRegionPopulated;
		public Action OnClear;

		#region UNITY

		protected override void Awake()
		{
			base.Awake();
			OnRegionPopulated += InstantiateRenderer;
		}

		#endregion

		public override void Reset()
		{
			base.Reset();
			ResetOlives();
		}

		public void ResetOlives()
		{
			fincasDictionary.Clear();
			iterations = 0;
			OnClear?.Invoke();

			Renderer.Clear();
		}

		public override void Run()
		{
			ResetDelaunay();
			ResetVoronoi();
			ResetOlives();
			if (animatedOlives)
			{
				animationCoroutine = StartCoroutine(RunCoroutine());
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
				voronoi.GenerateVoronoi();
				OnAllRegionsCreated();
				Vector2[] olivePositions = PopulateAllRegions();
				OnEndedGeneration?.Invoke(olivePositions);
			}
		}

		#region ANIMATION

		public bool animatedOlives = true;
		public int iterations;

		public bool Ended => fincasDictionary.Count >= voronoi.regions.Count;

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();

			if (animatedOlives)
			{
				while (!Ended)
				{
					Run_OneIteration();
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}

				OnEndedGeneration?.Invoke(OlivePositions);
			}
			else
			{
				Vector2[] olivePositions = PopulateAllRegions();
				OnEndedGeneration?.Invoke(olivePositions);
			}
		}

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended)
				delaunay.Run_OnePoint();
			else if (!voronoi.Ended)
				voronoi.Run_OneIteration();
			else if (!Ended)
				PopulateRegion(iterations++);
		}

		#endregion


		#region POPULATION

		private Vector2[] PopulateRegion(Polygon region)
		{
			// Bounds de la region
			var regionBounds = new Bounds2D(region.TransformVertices(Bounds.LocalToBoundsMatrix()));

			List<Vector2> olivos = new();

			for (float x = regionBounds.min.x; x < regionBounds.max.x; x += minSeparation)
			for (float y = regionBounds.min.y; y < regionBounds.max.y; y += minSeparation)
			{
				Vector2 pos = regionBounds.BoundsToLocalMatrix().MultiplyPoint3x4(new Vector2(x, y).ToV3xz()).ToV2xz();

				if (region.Contains_RayCast(pos))
					olivos.Add(pos);
			}

			Vector2[] olivosGenerated = olivos.ToArray();

			fincasDictionary.Add(region, olivosGenerated);

			OnRegionPopulated?.Invoke(olivosGenerated);

			return olivosGenerated;
		}

		private Vector2[] PopulateRegion(int index) => PopulateRegion(Regions[index]);

		private Vector2[] PopulateAllRegions() => Regions.SelectMany(PopulateRegion).ToArray();

		#endregion


		#region RENDERING

		private readonly bool _drawOlivos = true;

		private PointSpriteRenderer _spritesRenderer;
		private PointSpriteRenderer Renderer => _spritesRenderer ??= GetComponentInChildren<PointSpriteRenderer>(true);

		private Dictionary<Polygon, PointSpriteRenderer> spritesRendererDictionary = new();

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			_spritesRenderer ??= Renderer
			                     ?? UnityUtils.InstantiateEmptyObject(transform, "Olive Sprites Renderer")
				                     .AddComponent<PointSpriteRenderer>();

			Renderer.transform.ApplyMatrix(Bounds.LocalToBoundsMatrix());
			Renderer.transform.Translate(Vector3.up * 1);
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();
			InstantiateRenderer(OlivePositions);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			Renderer.UpdateGeometry(OlivePositions);
		}

		private void InstantiateRenderer(Vector2[] olivos)
		{
			Renderer.Instantiate(olivos, "Olivo");
			if (CanProjectOnTerrain && Terrain != null)
				Renderer.ProjectOnTerrain(Terrain);
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (!drawGizmos || !_drawOlivos) return;

			Gizmos.color = "#808000ff".ToUnityColor();

			foreach (Vector2 localPos in OlivePositions)
			{
				Vector3 pos = ToWorld(localPos);
				if (CanProjectOnTerrain)
					pos = Terrain.Project(pos);
				Gizmos.DrawSphere(pos + Vector3.up * 3, .1f);
			}
		}
#endif

		#endregion
	}
}
