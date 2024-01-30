using Cinemachine;
using UnityEngine;

namespace Map.Markers
{
    public class MarkersCamTargetGroup : MonoBehaviour
    {
        private CinemachineTargetGroup _targetGroup;

        private void Awake()
        {
            _targetGroup = GetComponent<CinemachineTargetGroup>();
        }

        private void Start()
        {
            MarkerManager.Instance.OnMarkerAdded += HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkerRemoved += HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkerMoved += HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkersClear += UpdateTargetGroup;
            UpdateTargetGroup();
        }

        private void OnDestroy()
        {
            MarkerManager.Instance.OnMarkerAdded -= HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkerRemoved -= HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkerMoved -= HandleOnAnyMarkerChange;
            MarkerManager.Instance.OnMarkersClear -= UpdateTargetGroup;
        }

        private void HandleOnAnyMarkerChange(Marker marker, int index)
        {
            UpdateTargetGroup();
        }

        private void UpdateTargetGroup()
        {
            foreach (var markerObject in FindObjectsOfType<MarkerObject>())
                _targetGroup.AddMember(markerObject.transform, 1, 1);
        }
    }
}