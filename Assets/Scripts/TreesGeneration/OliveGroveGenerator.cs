using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Generators;
using DavidUtils.Rendering;
using DavidUtils.TerrainExtensions;
using UnityEngine;

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
		public Vector2[] OlivePositions => fincasDictionary.SelectMany(pair => pair.Value).ToArray();
		public Vector2[] OlivePositionsByRegion(Polygon region) => fincasDictionary[region];


		public Action<Vector2[]> OnEndedGeneration;
		public Action<Vector2[]> OnRegionPopulated;
		public Action OnClear;

		public override void Reset()
		{
			base.Reset();
			fincasDictionary.Clear();
			iterations = 0;
			OnClear?.Invoke();
		}

		public override void Run()
		{
			Reset();
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
		
		public bool Ended => fincasDictionary.Count >= voronoi.regions.Count;

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();

			if (animatedOlives)
			{
				while (!Ended)
				{
					Run_OneIteration();
					yield return new WaitForSecondsRealtime(delayMilliseconds);
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
				PopulateRegion(iterations++);
		}

		#endregion
		

		#region POPULATION

		private Vector2[] PopulateRegion(Polygon region)
		{
			// TODO Implementar la generacion de olivos en una finca
			Vector2[] olives = { region.centroid };
			
			fincasDictionary.Add(region, olives);

			OnRegionPopulated?.Invoke(olives.ToArray());

			return olives;
		}

		private Vector2[] PopulateRegion(int index) => PopulateRegion(Regions[index]);

		private Vector2[] PopulateAllRegions() => Regions.SelectMany(PopulateRegion).ToArray();

		#endregion

		
		#region RENDERING

		private readonly bool _drawOlivos = true;
		
		private PointSpriteRenderer spritesRenderer = new();

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			spritesRenderer.Initialize(transform, "Olive Sprites Renderer");
			
			spritesRenderer.RenderParent.transform.localPosition = Bounds.min.ToV3xz();
			spritesRenderer.RenderParent.transform.localScale = Bounds.Size.ToV3xz().WithY(1);
			spritesRenderer.RenderParent.Translate(Vector3.up * 1);
		}
		
		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();
			spritesRenderer.Instantiate(OlivePositions, "Olivo");
			if (CanProjectOnTerrain && Terrain != null)
				spritesRenderer.ProjectOnTerrain(Terrain);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			spritesRenderer.Update(OlivePositions);
		}

		#endregion

		
		#region DEBUG
		
#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			
			if (!drawGizmos || !_drawOlivos) return;

			Gizmos.color = "#808000ff".ToUnityColor();

			foreach (Vector2 localPos in OlivePositions)
			{
				Vector3 pos = ToWorld(localPos);
				if (CanProjectOnTerrain)
					pos = Terrain.Project(pos);
				Gizmos.DrawSphere(pos + Vector3.up * 3, .1f);
			}
		}
#endif

		#endregion

	}
}
