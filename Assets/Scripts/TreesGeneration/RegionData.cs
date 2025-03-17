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
		
		public RegionData() {}
		public RegionData(Polygon polygon) => this.polygon = polygon;

		public virtual string Name => "Region";
	}
	
	
	[Serializable]
	public class TreesRegionData: RegionData
	{
		public const float DefaultScale = 10;
		
		public List<Vector2> treePositions = new();
		public int TreeCount => treePositions.Count;
		
		public float[] radiusByTree;
		public float Radius
		{
			get => radiusByTree.IsNullOrEmpty() ? DefaultScale : radiusByTree[0];
			set => radiusByTree = value.ToFilledArray(Math.Max(1, TreeCount)).ToArray();
		}
		
		public bool IsEmpty => treePositions.IsNullOrEmpty();
		
		public TreesRegionData(Polygon polygon, List<Vector2> treePositions = null, float radius = DefaultScale) 
			: this(polygon, treePositions, radius.ToFilledArray(Math.Max(1, treePositions?.Count ?? 1)).ToArray()) { }
		
		public TreesRegionData(Polygon polygon, List<Vector2> treePositions = null, float[] radius = null) : base(polygon)
		{
			this.treePositions = treePositions ?? new List<Vector2>();
			radiusByTree = radius ?? DefaultScale.ToFilledArray(Math.Max(1, TreeCount)).ToArray();
		}

		public override string Name => "Tree Region";
	}

	[Serializable]
	public class OliveRegionData: TreesRegionData
	{
		public override string Name => "Olivar";
		
		public OliveType oliveType;

		private List<Vector2> olivosInterior;
		private List<Vector2> olivosLinde;
		
		public List<Vector2> OlivosInterior
		{
			get => olivosInterior;
			set
			{
				olivosInterior = value;
				UpdateTreePositions();
			}
		}
		public List<Vector2> OlivosLinde
		{
			get => olivosLinde;
			set
			{
				olivosLinde = value;
				UpdateTreePositions();
			}
		}

		public IEnumerable<Vector2> Olivos => treePositions;
		public int OlivosCount => Olivos.Count();

		public void UpdateTreePositions() =>
			treePositions = (olivosInterior ??= new List<Vector2>()).Concat(olivosLinde ??= new List<Vector2>()).ToList();

		public Polygon interiorPolygon;

		public Vector2 orientation;
		

		// CON LINDE
		private OliveRegionData(
			Polygon polygon, OliveType oliveType, List<Vector2> olivosInterior, Vector2 orientation,
			List<Vector2> olivosLinde = null, Polygon interiorPolygon = null,
			float[] radius = null
		): base(polygon, olivosInterior.Concat(olivosLinde ?? new List<Vector2>()).ToList(), radius)
		{
			this.oliveType = oliveType;
			this.olivosInterior = olivosInterior;
			this.orientation = orientation;
			this.olivosLinde = olivosLinde ?? new List<Vector2>();
			this.interiorPolygon = interiorPolygon ?? polygon;
		}

		// Empty Region Data
		public OliveRegionData(Polygon polygon, OliveType oliveType, Vector2 orientation, Polygon interiorPolygon = null)
			: this(polygon, oliveType, new List<Vector2>(), orientation, new List<Vector2>(), interiorPolygon ?? polygon)
		{ }
	}

	[Serializable]
	public class ForestRegionData : RegionData
	{
		public override string Name => "Bosque";

		
	}
}
