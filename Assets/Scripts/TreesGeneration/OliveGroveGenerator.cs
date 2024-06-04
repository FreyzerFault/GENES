using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using DavidUtils.Rendering;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace TreesGeneration
{
	public class OliveGroveGenerator : VoronoiGenerator
	{
		private int NumFincas => numSeeds;

		// Parametros para el layout de una finca
		[Serializable]
		public struct FincaGenerationParams
		{
			[Range(0.1f, 10)] public float minSeparation;
			[Range(0.1f, 10)] public float minDistToLinde;
			[Range(0.1f, 30)] public float lindeWidth;
		}

		[FormerlySerializedAs("fincaGenerationParams")]
		[Space]
		[Header("OLIVAS")]
		public FincaGenerationParams fincaParams = new() { minSeparation = 5, minDistToLinde = 5, lindeWidth = 10 };


		public Vector2[] OlivePositions => _regionsData?.SelectMany(pair => pair.Value.olivosPoints).ToArray();
		public Vector2[] OlivePositionsByRegion(Polygon region) => _regionsData[region].olivosPoints;

		public Action<RegionData[]> OnEndedGeneration;
		public Action<RegionData> OnRegionPopulated;
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
			_regionsData.Clear();
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
				voronoi.GenerateVoronoi();
				PopulateAllRegions();
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

		public bool Ended => _regionsData.Count >= voronoi.regions.Count;

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

				OnEndedGeneration?.Invoke(_regionsData.Values.ToArray());
			}
			else
			{
				PopulateAllRegions();
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

		public struct RegionData
		{
			public Vector2[] olivosPoints;

			public Polygon polygon;
			public Polygon interiorPolygon;

			public Vector2 orientation;
			public Vector2 Centroid => polygon.centroid;
		}

		private readonly Dictionary<Polygon, RegionData> _regionsData = new();

		private void PopulateAllRegions()
		{
			Regions.ForEach(r => PopulateRegion(r));
			OnEndedGeneration?.Invoke(_regionsData.Values.ToArray());
		}

		private RegionData PopulateRegion(Polygon region)
		{
			Vector2 orientation = Random.insideUnitCircle;

			Vector2 lindeWidthLocal = WorldToLocalMatrix.MultiplyVector(Vector3.one * fincaParams.lindeWidth);
			Vector2 minSeparationLocal = WorldToLocalMatrix.MultiplyVector(Vector3.one * fincaParams.minSeparation);

			// minSeparationLocal.Rotate(Vector2.SignedAngle(orientation, Vector2.right));

			minSeparationLocal = new Vector2(
				Mathf.Max(0.001f, Mathf.Abs(minSeparationLocal.x)),
				Mathf.Max(0.001f, Mathf.Abs(minSeparationLocal.y))
			);

			var interiorPolygon = new Polygon(
				region.vertices.Select(v => v + (region.centroid - v).normalized * lindeWidthLocal),
				region.centroid
			);

			Vector2[] olivosGenerated = PopulatePolygon(interiorPolygon, minSeparationLocal, orientation);

			var data = new RegionData
			{
				olivosPoints = olivosGenerated,
				polygon = region,
				interiorPolygon = interiorPolygon,
				orientation = orientation
			};
			_regionsData.Add(region, data);

			OnRegionPopulated?.Invoke(data);
			return data;
		}

		/// <summary>
		///     Popula de olivos un polígono, calculando su OBB e iterando en los axis del OBB
		/// </summary>
		private static Vector2[] PopulatePolygon(Polygon region, Vector2 minSeparation, Vector2 orientation)
		{
			// OBB del polígono con la orientación de la hilera
			OBB_2D obb = new(region, orientation);

			// AABB rotado del OBB para posicionar los olivos de forma simple en una grid y luego rotarlos de vuelta
			// como si los hubieramos colocado en el OBB
			AABB_2D aabb = obb.AABB_Rotated;

			List<Vector2> olivos = new();

			// Iteramos en X e Y con la separacion dada en ambos ejes.
			// Solo se populan los puntos dentro del poligono
			for (float x = aabb.min.x; x < aabb.max.x; x += minSeparation.x)
			for (float y = aabb.min.y; y < aabb.max.y; y += minSeparation.y)
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
			InstantiateRenderer(_regionsData.Values.ToArray());
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			Renderer.UpdateGeometry(OlivePositions);
		}

		private void InstantiateRenderer(RegionData regionData)
		{
			Renderer.Instantiate(regionData.olivosPoints, "Olivo");
			if (CanProjectOnTerrain && Terrain != null)
				Renderer.ProjectOnTerrain(Terrain);
		}

		private void InstantiateRenderer(RegionData[] regionsData)
		{
			for (var i = 0; i < regionsData.Length; i++)
			{
				RegionData data = regionsData[i];
				Renderer.Instantiate(data.olivosPoints, $"Olivo {i}");
			}

			// Project ALL on Terrain
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
				foreach (KeyValuePair<Polygon, RegionData> pair in _regionsData)
				{
					Polygon region = pair.Key;
					RegionData data = pair.Value;

					OBB_2D obb = new(region, data.orientation);
					obb.DrawGizmos(LocalToWorldMatrix, Color.white, 3);

					foreach (Vector2 olivePosition in OlivePositions)
					{
						Gizmos.color = Color.green;
						Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivePosition), 2);
					}

					data.interiorPolygon.OnDrawGizmos(LocalToWorldMatrix, Color.grey);

					GizmosExtensions.DrawArrowWire(
						LocalToWorldMatrix.MultiplyPoint3x4(data.Centroid),
						data.orientation.ToV3xz(),
						Vector3.right,
						10,
						thickness: 10,
						color: Color.white
					);
				}
		}
#endif

		#endregion
	}
}
