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
	public enum CropType
	{
		Traditional,
		Intesive,
		SuperIntesive
	}

	public class OliveGroveGenerator : VoronoiGenerator
	{
		// Parametros para el layout de una finca
		[Serializable]
		public struct OlivoPopulationParams
		{
			[Range(0.1f, 10)] public float minSeparation;
			[Range(0.1f, 30)] public float lindeWidth;

			public CropType defaultCropType;
			public CropType[] Types => new[] { CropType.Traditional, CropType.Intesive, CropType.SuperIntesive };

			// % de cada tipo
			[Range(0, 1)] public float probTraditionalCrop;
			[Range(0, 1)] public float probIntensiveCrop;
			[Range(0, 1)] public float probSuperIntensiveCrop;

			private float[] Probabilities => new[] { probTraditionalCrop, probIntensiveCrop, probSuperIntensiveCrop };

			public CropType RandomizedType => Types.PickByProbability(Probabilities);
		}

		[Serializable]
		public struct RegionData
		{
			public CropType cropType;

			public List<Vector2> olivosInterior;
			public List<Vector2> olivosLinde;

			public Polygon polygon;
			public Polygon interiorPolygon;
			public Polygon[] lindeSections;

			public Vector2 orientation;
			public Vector2 Centroid => polygon.centroid;

			public IEnumerable<Vector2> Olivos => (olivosInterior ?? new List<Vector2>()).Concat(olivosLinde);

			private RegionData(
				CropType cropType, List<Vector2> olivosInterior, Polygon polygon, Vector2 orientation,
				List<Vector2> olivosLinde = null, Polygon? interiorPolygon = null, Polygon[] lindeSections = null
			)
			{
				this.cropType = cropType;
				this.olivosInterior = olivosInterior;
				this.polygon = polygon;
				this.orientation = orientation;
				this.olivosLinde = olivosLinde ?? new List<Vector2>();
				this.interiorPolygon = interiorPolygon ?? polygon;
				this.lindeSections = lindeSections ?? Array.Empty<Polygon>();
			}
		}

		private readonly Dictionary<Polygon, RegionData> _regionsData = new();

		private int NumFincas => numSeeds;

		[FormerlySerializedAs("fincaParams")]
		[Space]
		[Header("OLIVAS")]
		public OlivoPopulationParams populationParams = new()
		{
			probTraditionalCrop = 50,
			probIntensiveCrop = 50,
			probSuperIntensiveCrop = 0,
			minSeparation = 5, lindeWidth = 10
		};


		public IEnumerable<Vector2> OlivePositions => _regionsData?.SelectMany(pair => pair.Value.Olivos);
		public IEnumerable<Vector2> OlivePositionsByRegion(Polygon region) => _regionsData[region].Olivos;

		public Action<RegionData[]> OnEndedGeneration;
		public Action<RegionData> OnRegionPopulated;
		public Action OnClear;

		#region UNITY

		protected override void Awake()
		{
			base.Awake();
			OnRegionPopulated += InstantiateRenderer;
		}

		private void OnValidate()
		{
			float[] ratios =
			{
				populationParams.probTraditionalCrop,
				populationParams.probIntensiveCrop,
				populationParams.probSuperIntensiveCrop
			};

			float suma = ratios.Sum();
			if (suma <= 1) return;

			float reduccion = (suma - 1) / 3;

			populationParams.probTraditionalCrop -= reduccion;
			populationParams.probIntensiveCrop -= reduccion;
			populationParams.probSuperIntensiveCrop -= reduccion;
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

		private void PopulateAllRegions()
		{
			Regions.ForEach(r => PopulateRegion(r));
			OnEndedGeneration?.Invoke(_regionsData.Values.ToArray());
		}

		private RegionData PopulateRegion(Polygon region, CropType? cropType = null)
		{
			// CROP TYPE
			cropType ??= populationParams.RandomizedType;

			Vector2 orientation = Random.insideUnitCircle;

			Vector2 lindeWidthLocal = MeasureToLocalPositive(populationParams.lindeWidth);
			Vector2 minSeparationLocal = MeasureToLocalPositive(populationParams.minSeparation);

			float separation = Mathf.Max(minSeparationLocal.x, minSeparationLocal.y);

			Polygon interiorPolygon = region.InteriorPolygon(lindeWidthLocal);

			List<Vector2> olivosInterior = PopulatePolygon(interiorPolygon, minSeparationLocal, orientation).ToList();
			List<Vector2> olivosLinde = PopulateLinde(GetLinde(region, lindeWidthLocal), separation).ToList();

			var data = new RegionData
			{
				olivosInterior = olivosInterior,
				olivosLinde = olivosLinde,
				polygon = region,
				interiorPolygon = interiorPolygon,
				orientation = orientation,
				lindeSections = BuildLindePolygonSections(region, interiorPolygon),
				cropType = cropType.Value
			};

			data = PostprocessRegionData(data, separation);

			_regionsData.Add(region, data);

			OnRegionPopulated?.Invoke(data);
			return data;
		}

		/// <summary>
		///     Popula de olivos un polígono, calculando su OBB e iterando en los axis del OBB
		/// </summary>
		private static IEnumerable<Vector2> PopulatePolygon(Polygon region, Vector2 minSeparation, Vector2 orientation)
		{
			// OBB del polígono con la orientación de la hilera
			OBB_2D obb = new(region, orientation);

			// AABB rotado del OBB para posicionar los olivos de forma simple en una grid y luego rotarlos de vuelta
			// como si los hubieramos colocado en el OBB
			AABB_2D aabb = obb.AABB_Rotated;

			Vector2Int gridSize = Vector2Int.FloorToInt(aabb.Size / minSeparation);
			List<Vector2> olivos = new(gridSize.x * gridSize.y);

			// Iteramos en X e Y con la separacion dada en ambos ejes.
			// Solo se populan los puntos dentro del poligono
			for (float x = aabb.min.x; x < aabb.max.x; x += minSeparation.x)
			for (float y = aabb.min.y; y < aabb.max.y; y += minSeparation.y)
			{
				// Rotamos la posicion de vuelta al OBB y comprobamos si esta dentro del poligono
				Vector2 pos = new Vector2(x, y).Rotate(obb.Angle, obb.min);
				if (region.Contains_RayCast(pos)) olivos.Add(pos);
			}

			return olivos;
		}


		/// <summary>
		///     Aristas del centro de la Linde
		///     Se calculan reduciendo el polígono de la region por la mitad del ancho de la linde
		/// </summary>
		private static IEnumerable<Edge> GetLinde(Polygon region, Vector2 width) =>
			region.InteriorPolygon(width / 2).Edges;

		private static IEnumerable<Vector2> PopulateLinde(IEnumerable<Edge> linde, float minSeparation)
		{
			Vector2? lastOlivo = null;
			List<Vector2> olivosLinde = new();
			foreach (Edge edge in linde)
			{
				// Calculamos previamente el NUMERO de OLIVOS que caben
				int numOlivos = Mathf.FloorToInt(edge.Vector.magnitude / minSeparation) + 1;

				List<Vector2> olivos = new(numOlivos);

				// Antes de colocar el 1º olivo
				// Miramos el ultimo olivo colocado en la anterior arista
				// Si no están demasiado cerca, lo añadimos
				Vector2 firstOlivo = edge.begin;
				bool firstIsTooClose = lastOlivo.HasValue
				                       && Vector2.Distance(lastOlivo.Value, firstOlivo) < minSeparation;

				if (!firstIsTooClose) olivos.Add(firstOlivo);

				for (var i = 1; i < numOlivos; i++)
				{
					// INTERPOLACION [begin <- t = .?f -> end]
					float t = i * minSeparation / edge.Vector.magnitude;
					olivos.Add(Vector2.Lerp(edge.begin, edge.end, t));
				}

				lastOlivo = olivos.Last();

				olivosLinde.AddRange(olivos);
			}

			// El ultimo olivo podria estar muy cerca del primero
			if (Vector2.Distance(olivosLinde.Last(), olivosLinde.First()) < minSeparation)
				olivosLinde.RemoveAt(0);

			return olivosLinde;
		}

		/// <summary>
		///     Secciones poligonales de la linde, 1 por arista. Para visualizar y debuggear
		///     Recortamos la linde en secciones, una por arista.
		/// </summary>
		private static Polygon[] BuildLindePolygonSections(Polygon exterior, Polygon interior)
		{
			var lindeSections = new Polygon[exterior.VextexCount];
			for (var i = 0; i < exterior.VextexCount; i++)
			{
				Vector2 in0 = interior.vertices[i];
				Vector2 in1 = interior.vertices[(i + 1) % interior.VextexCount];
				Vector2 ex0 = exterior.vertices[i];
				Vector2 ex1 = exterior.vertices[(i + 1) % exterior.VextexCount];

				// CCW Vertices
				Vector2[] sectionVertices = { in0, in1, ex1, ex0 };
				lindeSections[i] = new Polygon(sectionVertices);
			}

			return lindeSections;
		}

		/// <summary>
		///     Postprocesado.
		///     Principalmente, elimino los olivos del interior que estén demasiado cerca de cualquiera de su linde
		/// </summary>
		private static RegionData PostprocessRegionData(RegionData data, float minSeparation)
		{
			data.olivosInterior.RemoveAll(
				olivo =>
					data.olivosLinde.Any(olivoLinde => Vector2.Distance(olivo, olivoLinde) < minSeparation)
			);
			return data;
		}

		#endregion


		#region SPACE CONVERSIONS

		private Vector2 VectorToLocalPositive(Vector3 vector)
		{
			Vector2 vectorlocal = WorldToLocalMatrix.MultiplyVector(vector);
			return Vector2.Max(vectorlocal.Abs(), Vector2.one * 0.001f);
		}

		private Vector2 VectorToLocalPositive(Vector2 vector) => VectorToLocalPositive(vector.ToV3xz());
		private Vector2 MeasureToLocalPositive(float value) => VectorToLocalPositive(Vector3.one * value);

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

		private void InstantiateRenderer(IEnumerable<RegionData> regionsData)
		{
			regionsData.ForEach((r, i) => Renderer.Instantiate(r.Olivos, $"Olivo {i}"));

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

			foreach (KeyValuePair<Polygon, RegionData> pair in _regionsData)
			{
				Polygon region = pair.Key;
				RegionData data = pair.Value;

				if (drawOBBs)
				{
					OBB_2D obb = new(region, data.orientation);
					obb.DrawGizmos(LocalToWorldMatrix, Color.white, 3);
				}

				var spheresRadius = 1f;

				Color oliveColor = "#4E8000".ToUnityColor();
				Color intensiveColor = "#028000".ToUnityColor();

				// POLIGONO INTERIOR
				Gizmos.color = data.cropType == CropType.Traditional ? oliveColor : intensiveColor;
				data.interiorPolygon.OnDrawGizmos(LocalToWorldMatrix, Color.green.Darken(.5f));
				data.olivosInterior.ForEach(
					olivo => Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivo), spheresRadius)
				);

				// LINDE
				if (data.cropType == CropType.Traditional)
				{
					Gizmos.color = Color.magenta;
					data.lindeSections.ForEach(
						lindeSection => lindeSection.OnDrawGizmos(
							LocalToWorldMatrix,
							Color.red.Darken(.5f),
							Color.red
						)
					);
					data.olivosLinde.ForEach(
						olivo => Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivo), spheresRadius)
					);
				}


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
