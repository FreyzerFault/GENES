using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TreesGeneration
{
	public static class OliveGroveGenerator
	{
		#region POPULATION

		/// <summary>
		///     Popula una región con olivos, según su forma (Polígono) y el tipo de cultivo.
		///     Si no se pasa un Tipo de Cultivo se elige uno aleatorio según las probabilidades de globalParams
		/// </summary>
		public static OliveRegionData PopulateRegion(Polygon region, BoundsComponent boundsComp, OliveGenSettings settings)
		{
			// CROP TYPE
			OliveType cropType = settings.RandomizedType;
			OliveTypeParams oliveParams = settings.GetCropTypeParams(cropType);

			// Separacion (Random [min - max])
			Vector2 separation = Vector2.Lerp(oliveParams.separationMin, oliveParams.separationMax, Random.value);
			Vector2 separationLocal = boundsComp.VectorToLocalPositive(separation);

			Vector2 orientation = GetRegionOrientation_ByAverage(region);

			OliveRegionData data = new(region, cropType, orientation);
			
			switch (cropType)
			{
				case OliveType.Traditional:
					// OLIVO EN TRADICIONAL => Añadimos la Linde
					Vector2 lindeWidthLocal = boundsComp.MeasureToLocalPositive(settings.lindeWidth);

					data.interiorPolygon = region.InteriorPolygon(lindeWidthLocal);

					data.olivosInterior = PopulatePolygon(data.interiorPolygon, separationLocal, orientation).ToList();
					data.olivosLinde = PopulateLinde(GetLinde(region, lindeWidthLocal), separationLocal.x).ToList();

					// Postprocesado
					data = PostprocessOliveRegionData(data, Mathf.Min(separationLocal.x, separationLocal.y));
					
					break;
				case OliveType.Intesive:
				case OliveType.SuperIntesive:
					// OLIVO EN INTENSIVO => NO hay Linde, pero procuramos una separacion con sus vecinos
					float margin = separationLocal.y / 2;
					Polygon marginPolygon = region.InteriorPolygon(margin);
					data.olivosInterior = PopulatePolygon(marginPolygon, separationLocal, orientation).ToList();
					break;
			}
			
			data.olivosInterior.RemoveAll(p => IsInvalidPosition(p, settings));
			data.olivosLinde?.RemoveAll(p => IsInvalidPosition(p, settings));
			
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

			const int maxPolygons = 10000;

			// Iteramos en X e Y con la separacion dada en ambos ejes.
			// Solo se populan los puntos dentro del poligono
			for (float x = aabb.min.x; x < aabb.max.x; x += minSeparation.x)
			for (float y = aabb.min.y; y < aabb.max.y; y += minSeparation.y)
			{
				// Rotamos la posicion de vuelta al OBB y comprobamos si esta dentro del poligono
				Vector2 pos = new Vector2(x, y).Rotate(obb.Angle, aabb.min);
				bool insidePolygon = region.Contains_RayCast(pos);
				
				if (insidePolygon) olivos.Add(pos);

				if (olivos.Count <= maxPolygons) continue;
				Debug.LogWarning($"Max Polygons reached ({maxPolygons})");
				return olivos;
			} 

			return olivos;
		}

		
		private static bool IsInvalidPosition(Vector2 normPos, OliveGenSettings settings)
		{
			// Outside [0,1] Bounds
			if (!normPos.IsIn01()) return true;
            
			// Check Slope is too high
			Terrain terrain = Terrain.activeTerrain;
			if (terrain == null) return false;
			
			float maxSlope = settings.maxSlopeAngle;
			float angle = Vector3.Angle(terrain.GetNormal(normPos), Vector3.up);
			return angle > maxSlope;
		}
		
		
		public static Vector2 GetRegionOrientation_ByCentroid(Polygon region)
		{
			Terrain terrain = Terrain.activeTerrain;
			
			// Si no hay terreno, se coge una orientacion random para testear
			if (terrain == null) return Random.insideUnitCircle;
			
			// Cogemos la orientacion del olivar
			// Como la pendiente del terreno en la posicion del centroide
			Vector3 terrainNormal = terrain.GetNormal(region.centroid);
			Vector2 slope =  terrainNormal.ToV2xz().normalized;
			return Vector2.Perpendicular(slope);
		}
		
		public static Vector2 GetRegionOrientation_ByAverage(Polygon region)
		{
			Terrain terrain = Terrain.activeTerrain;
			
			// Si no hay terreno, se coge una orientacion random para testear
			if (terrain == null) return Random.insideUnitCircle;
			
			// Cogemos la orientacion del olivar
			// Como la media de pendientes en toda la region
			
			// Para ello hacemos un sampleo del AABB de la region ignorando posiciones fuera del polígono
			const float sampleIncrement = 0.01f;
			var aabb = new AABB_2D(region);
			
			Vector2 slope;
			Vector2 sampleSum = Vector2.zero;
			List<Vector2> samples = new();
			
			for (float x = aabb.min.x; x < aabb.max.x; x += sampleIncrement)
			for (float y = aabb.min.y; y < aabb.max.y; y += sampleIncrement)
			{
				var pos = new Vector2(x, y);
				if (!region.Contains_RayCast(pos)) continue;
				slope = terrain.GetNormal(pos).ToV2xz().normalized;
				sampleSum += slope;
				samples.Add(slope);
			}
			
			// AVERAGE slope
			slope = sampleSum / (aabb.Size.x * aabb.Size.y);
			return Vector2.Perpendicular(slope);
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
		private static OliveRegionData PostprocessOliveRegionData(OliveRegionData data, float minSeparation)
		{
			data.olivosInterior.RemoveAll(
				olivo =>
					data.olivosLinde.Any(olivoLinde => Vector2.Distance(olivo, olivoLinde) < minSeparation)
			);
			return data;
		}

		#endregion

		#endregion
		

		#region DEBUG

#if UNITY_EDITOR

		public static void DrawGizmos(RegionData[] oliveData, Matrix4x4 localToWorldMatrix, bool drawOBBs = false)
		{;

			foreach (OliveRegionData data in oliveData.OfType<OliveRegionData>())
			{
				if (drawOBBs)
				{
					OBB_2D obb = new(data.polygon, data.orientation);
					obb.DrawGizmos(localToWorldMatrix, Color.white, 3);
				}

				Color oliveColor = "#4E8000".ToUnityColor();
				Color intensiveColor = "#028000".ToUnityColor();
				Color floorColor = "#948159".ToUnityColor();

				// OLIVOS
				Gizmos.color = data.oliveType == OliveType.Traditional ? oliveColor : intensiveColor;
				data.Olivos.ForEach(
					(olivo, i) =>
						Gizmos.DrawSphere(localToWorldMatrix.MultiplyPoint3x4(olivo), data.radiusByPoint[i])
				);


				// POLIGONO
				data.polygon.DrawGizmos(
					localToWorldMatrix,
					floorColor,
					floorColor.Lighten(.2f)
				);

				// LINDE
				if (data.oliveType == OliveType.Traditional)
					data.interiorPolygon.DrawGizmos(
						localToWorldMatrix,
						floorColor.RotateHue(.1f),
						floorColor.Lighten(.2f)
					);

				// ORIENTACION (flechas)
				float arrowSize = localToWorldMatrix.lossyScale.x / 20;
				GizmosExtensions.DrawArrow(
					GizmosExtensions.ArrowCap.Triangle,
					localToWorldMatrix.MultiplyPoint3x4(data.Centroid),
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
