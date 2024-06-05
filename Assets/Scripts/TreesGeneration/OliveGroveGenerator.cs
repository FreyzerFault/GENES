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


		public Vector2[] OlivePositions => _regionsData?.SelectMany(pair => pair.Value.Olivos).ToArray();
		public Vector2[] OlivePositionsByRegion(Polygon region) => _regionsData[region].Olivos;

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
			public Vector2[] olivosInterior;
			public Vector2[] olivosLinde;

			public Polygon polygon;
			public Polygon interiorPolygon;
			public Polygon[] lindeSections;

			public Vector2 orientation;
			public Vector2 Centroid => polygon.centroid;

			public Vector2[] Olivos => olivosInterior.Concat(olivosLinde).ToArray();
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

			minSeparationLocal = new Vector2(
				Mathf.Max(0.001f, Mathf.Abs(minSeparationLocal.x)),
				Mathf.Max(0.001f, Mathf.Abs(minSeparationLocal.y))
			);

			lindeWidthLocal = new Vector2(
				Mathf.Max(0.001f, Mathf.Abs(lindeWidthLocal.x)),
				Mathf.Max(0.001f, Mathf.Abs(lindeWidthLocal.y))
			);

			Polygon interiorPolygon = region.InteriorPolygon(lindeWidthLocal);

			Vector2[] olivosInterior = PopulatePolygon(interiorPolygon, minSeparationLocal, orientation);


			// LINDE

			// Polígono cuyas aristas son el centro de la Linde
			Polygon lindeCenter = region.InteriorPolygon(lindeWidthLocal / 2);

			// Recortamos la linde en secciones, una por arista.
			// Los vertices son [interior0, interior1, exterior1, exterior0]
			var lindeSections = new Polygon[region.VextexCount];
			for (var i = 0; i < region.VextexCount; i++)
			{
				Vector2 inVertex0 = interiorPolygon.vertices[i];
				Vector2 inVertex1 = interiorPolygon.vertices[(i + 1) % interiorPolygon.VextexCount];
				Vector2 exterior0 = region.vertices[i];
				Vector2 exterior1 = region.vertices[(i + 1) % region.VextexCount];

				// CCW Vertices
				Vector2[] sectionVertices = { inVertex0, inVertex1, exterior1, exterior0 };
				lindeSections[i] = new Polygon(sectionVertices);
			}

			Vector2[] olivosLinde = lindeSections
				.SelectMany(
					section =>
					{
						Vector2 lindeOrientation = section.Edges.ElementAt(1).Dir;
						lindeOrientation = WorldToLocalMatrix.MultiplyVector(lindeOrientation.ToV3xz());

						lindeOrientation = new Vector2(
							Mathf.Max(0.001f, Mathf.Abs(lindeOrientation.x)),
							Mathf.Max(0.001f, Mathf.Abs(lindeOrientation.y))
						);
						return PopulatePolygon(section, minSeparationLocal, lindeOrientation);
					}
				)
				.ToArray();

			var data = new RegionData
			{
				olivosInterior = olivosInterior,
				olivosLinde = olivosLinde,
				polygon = region,
				interiorPolygon = interiorPolygon,
				orientation = orientation,
				lindeSections = lindeSections
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
			Renderer.Instantiate(regionData.Olivos, "Olivo");
			if (CanProjectOnTerrain && Terrain != null)
				Renderer.ProjectOnTerrain(Terrain);
		}

		private void InstantiateRenderer(RegionData[] regionsData)
		{
			for (var i = 0; i < regionsData.Length; i++)
			{
				RegionData data = regionsData[i];
				Renderer.Instantiate(data.Olivos, $"Olivo {i}");
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

					var spheresRadius = .5f;

					// POLIGONO INTERIOR
					Gizmos.color = Color.green;
					data.interiorPolygon.OnDrawGizmos(LocalToWorldMatrix, Color.green.Darken(.5f));
					data.olivosInterior.ForEach(
						olivo => Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivo), spheresRadius)
					);

					// LINDE
					Gizmos.color = Color.magenta;
					data.lindeSections.ForEach(
						lindeSection => lindeSection.OnDrawGizmos(LocalToWorldMatrix, Color.red.Darken(.5f), Color.red)
					);
					data.olivosLinde.ForEach(
						olivo => Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivo), spheresRadius)
					);


					// ORIENTACION (flechas)
					GizmosExtensions.DrawArrowWire(
						LocalToWorldMatrix.MultiplyPoint3x4(data.Centroid),
						data.orientation.normalized.ToV3xz(),
						Vector3.right,
						10,
						thickness: 2,
						color: Color.white
					);
				}
		}
#endif

		#endregion
	}
}
