using Cinemachine;
using Map;
using Map.Rendering;
using UnityEngine;

namespace CameraManagement
{
    public class MarkersCamTargetGroup : MonoBehaviour
    {
        [SerializeField] private float firstMarkerWeight = 2;

        [SerializeField] private float defaultmarkerWeight = 1;

        private CinemachineTargetGroup _targetGroup;

        private MarkerObject[] MarkerObjs => FindObjectsOfType<MarkerObject>();

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
            if (MarkerManager.Instance == null) return;
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
            var markers = FindObjectsOfType<MarkerObject>();
            for (var i = 0; i < markers.Length; i++)
                _targetGroup.AddMember(
                    markers[i].transform,
                    // 1º Marker has more weigth
                    i == 0
                        ? firstMarkerWeight
                        : defaultmarkerWeight,
                    1
                );
        }
    }
}