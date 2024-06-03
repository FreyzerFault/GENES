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
using UnityEngine;
using Random = UnityEngine.Random;

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

		public bool AnimatedOlives
		{
			get => animatedOlives;
			set => animatedOlives = value;
		}

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
				PopulateRegion(Regions[iterations++]);
		}

		#endregion


		#region POPULATION

		private struct PopulationInfo
		{
			public Vector2 orientation;
		}

		private readonly Dictionary<Polygon, PopulationInfo> populationInfo = new();

		private Vector2[] PopulateAllRegions() => Regions.SelectMany(PopulateRegion).ToArray();

		private Vector2[] PopulateRegion(Polygon region)
		{
			Vector2 orientation = Random.insideUnitCircle;

			// DEBUG INFO
			populationInfo.Add(region, new PopulationInfo { orientation = orientation });

			Vector2[] olivosGenerated = PopulateRegion(AABB, region, minSeparation, orientation);
			fincasDictionary.Add(region, olivosGenerated);
			OnRegionPopulated?.Invoke(olivosGenerated);
			return olivosGenerated;
		}

		private static Vector2[] PopulateRegion(
			AABB_2D terrainAABB, Polygon region, float minSeparation, Vector2 orientation
		)
		{
			// OBB del polígono con la orientación de la hilera
			OBB_2D obb = new(region, orientation);

			// AABB rotado del OBB para posicionar los olivos de forma simple en una grid y luego rotarlos de vuelta
			// como si los hubieramos colocado en el OBB
			AABB_2D aabb = obb.AABB_Rotated;

			List<Vector2> olivos = new();

			// World measure to Local measure in Terrain AABB
			Vector2 minSeparationLocal =
				new Vector2(minSeparation, minSeparation).ScaleBy(terrainAABB.BoundsToLocalMatrix(false).lossyScale);

			// Iteramos en X e Y con la separacion dada en ambos ejes.
			// Solo se populan los puntos dentro del poligono
			for (float x = aabb.min.x; x < aabb.max.x; x += minSeparationLocal.x)
			for (float y = aabb.min.y; y < aabb.max.y; y += minSeparationLocal.y)
			{
				// Rotamos la posicion de vuelta al OBB y comprobamos si esta dentro del poligono
				Vector2 pos = new Vector2(x, y).Rotate(obb.Angle, obb.min);
				if (region.Contains_RayCast(pos)) olivos.Add(pos);
			}

			return olivos.ToArray();
		}

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

			BoundsComp.AdjustTransformToBounds(Renderer);
			Renderer.transform.localPosition += Vector3.up * .1f;
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

		public bool drawOBBs = true;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (!drawGizmos || !_drawOlivos || Regions.IsNullOrEmpty()) return;

			if (drawOBBs)
				foreach (KeyValuePair<Polygon, PopulationInfo> pair in populationInfo)
				{
					Polygon region = pair.Key;
					PopulationInfo info = pair.Value;

					OBB_2D obb = new(region, info.orientation);
					obb.DrawGizmos(LocalToWorldMatrix, Color.white, 3);
				}
		}
#endif

		#endregion
	}
}
