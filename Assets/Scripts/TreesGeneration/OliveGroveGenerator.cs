using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.DevTools.Reflection;
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
		private readonly Dictionary<Polygon, RegionData> _regionsData = new();
		public RegionData[] Data => _regionsData.Values.ToArray();

		[ExposedField]
		public int NumFincas
		{
			get => numSeeds;
			set => numSeeds = value;
		}

		[Space]
		[Header("PARÁMETROS OLIVAR")]
		public GenerationSettingsSO genSettings;


		public IEnumerable<Vector2> OlivePositions => _regionsData?.SelectMany(pair => pair.Value.Olivos);
		public IEnumerable<Vector2> OlivePositionsByRegion(Polygon region) => _regionsData[region].Olivos;

		public Action<RegionData[]> OnEndedGeneration;
		public Action<RegionData> OnRegionPopulated;
		public Action OnClear;

		#region UNITY

		private void OnEnable() => OnRegionPopulated += HandleOnRegionPopulated;
		private void OnDisable() => OnRegionPopulated -= HandleOnRegionPopulated;

		private void HandleOnRegionPopulated(RegionData regionData)
		{
			InstantiateRenderer(regionData);
			if (CanProjectOnTerrain) ProjectOnTerrain();
		}

		#endregion


		#region GENERATION PIPELINE

		public override void Reset()
		{
			base.Reset();
			ResetOlives();
		}

		private void ResetOlives()
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

		#endregion


		#region ANIMATION

		public bool animatedOlives = true;
		public int iterations;

		public bool AnimatedOlives
		{
			get => animatedOlives;
			set => animatedOlives = value;
		}

		public bool Ended => _regionsData.Count >= voronoi.regions.Count;

		public override IEnumerator RunCoroutine()
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

		/// <summary>
		///     Popula una región con olivos, según su forma (Polígono) y el tipo de cultivo.
		///     Si no se pasa un Tipo de Cultivo se elige uno aleatorio según las probabilidades de globalParams
		/// </summary>
		private RegionData PopulateRegion(Polygon region, CropType? cropType = null)
		{
			// CROP TYPE
			cropType ??= genSettings.globalParams.RandomizedType;
			CropTypeParams cropParams = genSettings.GetCropTypeParams(cropType.Value);

			// Separacion (Random [min - max])
			Vector2 separation = Vector2.Lerp(cropParams.separationMin, cropParams.separationMax, Random.value);
			Vector2 separationLocal = VectorToLocalPositive(separation);

			// TODO Coger la Pendiente del Terreno como Perpendicular de la orientacion
			Vector2 orientation = Random.insideUnitCircle;

			RegionData data = new();
			switch (cropType)
			{
				case CropType.Traditional:
					// OLIVO EN TRADICIONAL => Añadimos la Linde
					Vector2 lindeWidthLocal = MeasureToLocalPositive(genSettings.globalParams.lindeWidth);

					Polygon interiorPolygon = region.InteriorPolygon(lindeWidthLocal);

					List<Vector2> olivosInterior =
						PopulatePolygon(interiorPolygon, separationLocal, orientation).ToList();
					List<Vector2> olivosLinde =
						PopulateLinde(GetLinde(region, lindeWidthLocal), separationLocal.x).ToList();

					data = new RegionData
					{
						olivosInterior = olivosInterior,
						olivosLinde = olivosLinde,
						polygon = region,
						interiorPolygon = interiorPolygon,
						orientation = orientation,
						cropType = cropType.Value
					};

					data = PostprocessRegionData(data, Mathf.Min(separationLocal.x, separationLocal.y));
					break;
				case CropType.Intesive:
				case CropType.SuperIntesive:
					// OLIVO EN INTENSIVO => NO hay Linde, pero procuramos una separacion con sus vecinos
					float margin = separationLocal.y / 2;
					Polygon marginPolygon = region.InteriorPolygon(margin);
					List<Vector2> olivos = PopulatePolygon(marginPolygon, separationLocal, orientation).ToList();

					data = new RegionData
					{
						olivosInterior = olivos,
						polygon = region,
						orientation = orientation,
						cropType = cropType.Value
					};
					break;
			}

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
				Vector2 pos = new Vector2(x, y).Rotate(obb.Angle, aabb.min);
				if (region.Contains_RayCast(pos)) olivos.Add(pos);
			}

			return olivos;
		}


		#region LINDE

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

				if (olivos.Count == 0)
					continue;

				lastOlivo = olivos.Last();

				olivosLinde.AddRange(olivos);
			}

			// El ultimo olivo podria estar muy cerca del primero
			if (Vector2.Distance(olivosLinde.Last(), olivosLinde.First()) < minSeparation)
				olivosLinde.RemoveAt(0);

			return olivosLinde;
		}

		#endregion


		#region POSTPROCESSING

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
			Renderer.scale = genSettings.GetCropTypeParams(regionData.cropType).scale;
			Renderer.Instantiate(regionData.Olivos, "Olivo");
		}

		private void InstantiateRenderer(IEnumerable<RegionData> regionsData)
		{
			regionsData.ForEach(InstantiateRenderer);
			if (CanProjectOnTerrain) ProjectOnTerrain();
		}

		// Project ALL on Terrain		
		private void ProjectOnTerrain()
		{
			if (Terrain == null) return;
			Renderer.ProjectOnTerrain(Terrain);
		}

		protected override void PositionRenderer()
		{
			base.PositionRenderer();
			Renderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			Renderer.transform.localScale = Vector3.one;
			BoundsComp.AdjustTransformToBounds(Renderer);
			Renderer.transform.localPosition += Vector3.back * .1f;
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

				float spheresRadius = genSettings.GetCropTypeParams(data.cropType).scale / 2;

				Color oliveColor = "#4E8000".ToUnityColor();
				Color intensiveColor = "#028000".ToUnityColor();
				Color floorColor = "#948159".ToUnityColor();

				// OLIVOS
				Gizmos.color = data.cropType == CropType.Traditional ? oliveColor : intensiveColor;
				data.Olivos.ForEach(
					olivo => Gizmos.DrawSphere(LocalToWorldMatrix.MultiplyPoint3x4(olivo), spheresRadius)
				);


				// POLIGONO
				data.polygon.DrawGizmos(
					LocalToWorldMatrix,
					floorColor,
					floorColor.Lighten(.2f)
				);

				// LINDE
				if (data.cropType == CropType.Traditional)
					data.interiorPolygon.DrawGizmos(
						LocalToWorldMatrix,
						floorColor.RotateHue(.1f),
						floorColor.Lighten(.2f)
					);

				// ORIENTACION (flechas)
				float arrowSize = AABB.Width / 20;
				GizmosExtensions.DrawArrow(
					GizmosExtensions.ArrowCap.Triangle,
					LocalToWorldMatrix.MultiplyPoint3x4(data.Centroid),
					data.orientation.normalized.ToV3xz() * arrowSize,
					Vector3.up,
					arrowSize / 3,
					thickness: 2,
					color: Color.white
				);
			}
		}
#endif

		#endregion
	}
}
