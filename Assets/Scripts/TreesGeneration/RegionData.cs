using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;
using UnityEngine.Serialization;

namespace TreesGeneration
{
	public enum OliveType
	{
		Traditional,
		Intesive,
		SuperIntesive
	}

	// Parametros para el layout de una finca
	[Serializable]
	public struct OliveGenParams
	{
		// MAIN PARAMETERS
		[Range(0.1f, 30)] public float lindeWidth;
		[Range(1, 90)] public float maxSlopeAngle;

		// TIPO de CULTIVO
		public OliveType defaultOliveType;
		public OliveType[] Types => new[] { OliveType.Traditional, OliveType.Intesive, OliveType.SuperIntesive };

		// % de cada tipo
		[Range(0, 1)] public float probTraditionalCrop;
		[Range(0, 1)] public float probIntensiveCrop;
		[Range(0, 1)] public float probSuperIntensiveCrop;

		public float[] Probabilities
		{
			get => new[] { probTraditionalCrop, probIntensiveCrop, probSuperIntensiveCrop };
			set
			{
				if (value.Length != 3) throw new ArgumentException("Probabilities must have 3 values");
				probTraditionalCrop = value[0];
				probIntensiveCrop = value[1];
				probSuperIntensiveCrop = value[2];
			}
		}

		public OliveType RandomizedType => Types.PickByProbability(Probabilities);

		public void NormalizeProbabilities() => Probabilities = Probabilities.NormalizeProbabilities().ToArray();
	}

	[Serializable]
	public struct OliveTypeParams
	{
		public OliveType type;
		[FormerlySerializedAs("separation")]
		// X: Separacion entre olivos, Y: Separacion entre hileras
		public Vector2 separationMin;
		public Vector2 separationMax;
		public float scale;
	}

	[Serializable]
	public struct RegionData
	{
		[FormerlySerializedAs("cropType")] public OliveType oliveType;

		public List<Vector2> olivosInterior;
		public List<Vector2> olivosLinde;

		public Polygon polygon;
		public Polygon interiorPolygon;

		public Vector2 orientation;
		public Vector2 Centroid => polygon.centroid;

		public IEnumerable<Vector2> Olivos =>
			(olivosInterior ??= new List<Vector2>()).Concat(olivosLinde ??= new List<Vector2>());

		private RegionData(
			OliveType oliveType, List<Vector2> olivosInterior, Polygon polygon, Vector2 orientation,
			List<Vector2> olivosLinde = null, Polygon? interiorPolygon = null
		)
		{
			this.oliveType = oliveType;
			this.olivosInterior = olivosInterior;
			this.polygon = polygon;
			this.orientation = orientation;
			this.olivosLinde = olivosLinde ?? new List<Vector2>();
			this.interiorPolygon = interiorPolygon ?? polygon;
		}
	}
}
