using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Generators;
using UnityEngine;

namespace TreesGeneration
{
	public class OliveGroveGenerator : VoronoiGenerator
	{
		[SerializeField] private int NumFincas => numSeeds;

		// Parametros para el layout de una finca
		[SerializeField] private float minSeparation = 5;
		[SerializeField] private float minDistToBoundary = 5;
		[SerializeField] private float boundaryOffset = 10;

		public Dictionary<Polygon, Vector2[]> fincasDictionary = new();
		public Vector2[] OlivePositions => fincasDictionary.SelectMany(pair => pair.Value).ToArray();
		public Vector2[] OlivePositionsByRegion(Polygon region) => fincasDictionary[region];

		public bool animatedOlives = true;

		public Action<Vector2[]> OnEndedGeneration;
		public Action<Vector2[]> OnRegionPopulated;
		public Action OnClear;

		public override void Reset()
		{
			base.Reset();
			fincasDictionary.Clear();
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

		public bool Ended => fincasDictionary.Count == voronoi.regions.Count;
		public int iterations;

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

		#region POPULATION

		private Vector2[] PopulateRegion(Polygon region)
		{
			Vector2[] olives = Array.Empty<Vector2>();

			// TODO

			fincasDictionary.Add(region, olives.ToArray());

			OnRegionPopulated?.Invoke(olives.ToArray());

			return olives;
		}

		private Vector2[] PopulateRegion(int index) => PopulateRegion(Regions[index]);

		private Vector2[] PopulateAllRegions() => Regions.SelectMany(PopulateRegion).ToArray();

		#endregion

		#region RENDERING

		private readonly bool _drawOlivos = true;

		#endregion


#if UNITY_EDITOR

		#region DEBUG

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			if (!_drawOlivos) return;

			Gizmos.color = "#808000ff".ToUnityColor();

			foreach (Vector2 localPos in OlivePositions)
			{
				Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(localPos.ToV3xz());
				Gizmos.DrawSphere(pos + Vector3.up * 3, 1f);
			}
		}

		#endregion

#endif
	}
}
