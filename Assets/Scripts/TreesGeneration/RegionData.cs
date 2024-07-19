using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;

namespace GENES.TreesGeneration
{
	public enum RegionType { Olive, Forest }
	
	[Serializable]
	public class RegionData
	{
		public RegionType type;
		
		public Polygon polygon;
		public Vector2 Centroid => polygon.centroid;
		
		public RegionData() => polygon = Polygon.Empty;
		public RegionData(Polygon polygon) => this.polygon = polygon;

		public virtual string Name => "Region";
	}

	[Serializable]
	public class OliveRegionData: RegionData
	{
		public const float DefaultScale = 1;
		public override string Name => "Olivar";
		
		public OliveType oliveType;

		public List<Vector2> olivosInterior;
		public List<Vector2> olivosLinde;

		public Polygon interiorPolygon;

		public Vector2 orientation;
		 public float[] radiusByPoint;

		public IEnumerable<Vector2> Olivos =>
			(olivosInterior ??= new List<Vector2>()).Concat(olivosLinde ??= new List<Vector2>());

		// CON LINDE
		private OliveRegionData(
			Polygon polygon, OliveType oliveType, List<Vector2> olivosInterior, Vector2 orientation,
			List<Vector2> olivosLinde = null, Polygon? interiorPolygon = null,
			float[] radius = null
		): base(polygon)
		{
			this.oliveType = oliveType;
			this.olivosInterior = olivosInterior;
			this.orientation = orientation;
			this.olivosLinde = olivosLinde ?? new List<Vector2>();
			this.interiorPolygon = interiorPolygon ?? polygon;
			radiusByPoint = radius ?? DefaultScale.ToFilledArray(Olivos.Count()).ToArray();
		}

		// Empty Region Data
		public OliveRegionData(Polygon polygon, OliveType oliveType, Vector2 orientation, Polygon? interiorPolygon = null)
			: this(polygon, oliveType, new List<Vector2>(), orientation, new List<Vector2>(), interiorPolygon ?? polygon)
		{ }
	}

	[Serializable]
	public class ForestRegionData : RegionData
	{
		public override string Name => "Bosque";
		
	}
}
