using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Geometry;
using UnityEngine;

namespace TreesGeneration
{
	[Serializable]
	public class RegionData
	{
	}

	[Serializable]
	public class OliveRegionData: RegionData
	{
		public OliveType oliveType;

		public List<Vector2> olivosInterior;
		public List<Vector2> olivosLinde;

		public Polygon polygon;
		public Polygon interiorPolygon;

		public Vector2 orientation;
		public Vector2 Centroid => polygon.centroid;

		public IEnumerable<Vector2> Olivos =>
			(olivosInterior ??= new List<Vector2>()).Concat(olivosLinde ??= new List<Vector2>());

		private OliveRegionData(
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

		// Empty Region Data
		public OliveRegionData(OliveType oliveType, Polygon polygon, Vector2 orientation, Polygon? interiorPolygon = null)
			: this(oliveType, new List<Vector2>(), polygon, orientation, new List<Vector2>(), interiorPolygon ?? polygon)
		{ }
	}
}
