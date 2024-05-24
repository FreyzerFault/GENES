using System.Linq;
using UnityEngine;

namespace Markers.Rendering
{
	public class MarkersCamTargetGroup : CamTargetGroupHandler
	{
		[SerializeField] private MarkerRenderer3D markerRenderer;

		private void Start() => markerRenderer ??= FindObjectOfType<MarkerRenderer3D>();

		private void OnEnable()
		{
			markerRenderer.OnMarkerSpawned += HandleOnMarkerSpawned;
			markerRenderer.OnMarkerDestroyed += HandleOnMarkerDestroyed;

			UpdateTargetGroup();
		}

		private void OnDisable()
		{
			markerRenderer.OnMarkerSpawned -= HandleOnMarkerSpawned;
			markerRenderer.OnMarkerDestroyed -= HandleOnMarkerDestroyed;
		}

		private void HandleOnMarkerSpawned(MarkerObject markerObj) => AddTarget(markerObj.transform);
		private void HandleOnMarkerDestroyed(MarkerObject markerObj) => RemoveTarget(markerObj.transform);

		private void UpdateTargetGroup()
		{
			if (markerRenderer == null) return;
			UpdateTargetGroup(markerRenderer.markerObjects.Select(m => m.transform).ToArray());
		}
	}
}
