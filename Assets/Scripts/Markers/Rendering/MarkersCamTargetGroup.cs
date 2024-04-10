using Cinemachine;
using UnityEngine;

namespace Markers.Rendering
{
    public class MarkersCamTargetGroup : MonoBehaviour
    {
        [SerializeField] private float firstMarkerWeight = 2;
        [SerializeField] private float defaultmarkerWeight = 1;

        private CinemachineTargetGroup _targetGroup;

        private MarkerRenderer3D markerRenderer;

        private void Awake() => _targetGroup = GetComponent<CinemachineTargetGroup>();

        private void Start()
        {
            markerRenderer = FindObjectOfType<MarkerRenderer3D>();
            
            markerRenderer.OnMarkerSpawned += HandleOnMarkerSpawned;
            markerRenderer.OnMarkerDestroyed += HandleOnMarkerDestroyed;
            
            UpdateTargetGroup();
        }

        private void OnDestroy()
        {
            markerRenderer.OnMarkerSpawned -= HandleOnMarkerSpawned;
            markerRenderer.OnMarkerDestroyed -= HandleOnMarkerDestroyed;
        }
        
        private void HandleOnMarkerSpawned(MarkerObject markerObj) => 
            _targetGroup.AddMember(
                markerObj.transform,
                _targetGroup.m_Targets.Length == 1 ? firstMarkerWeight : defaultmarkerWeight, 
                10
                );
        private void HandleOnMarkerDestroyed(MarkerObject markerObj) => _targetGroup.RemoveMember(markerObj.transform);

        private void UpdateTargetGroup()
        {
            if (markerRenderer == null) return;
            
            for (var i = 0; i < markerRenderer.markerObjects.Count; i++)
            {
                MarkerObject markerObj = markerRenderer.markerObjects[i];
                float weight = i == 0 ? firstMarkerWeight : defaultmarkerWeight;
                _targetGroup.AddMember(markerObj.transform, weight, 10);
            }
        }
    }
}