using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapInputController : MonoBehaviour
    {
        [SerializeField] private GameObject minimapParent;
        [SerializeField] private GameObject fullScreenMapParent;

        private MapState MapState => MapManager.Instance.MapState;
        private int MarkersSelectedCount => MarkerManager.Instance.SelectedCount;

        private void Start()
        {
            HandleStateChanged(MapState);
            MapManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            MapManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(MapState state)
        {
            fullScreenMapParent.SetActive(false);
            minimapParent.SetActive(false);

            switch (state)
            {
                case MapState.Fullscreen:
                    fullScreenMapParent.SetActive(true);
                    break;
                case MapState.Minimap:
                    minimapParent.SetActive(true);
                    break;
                case MapState.Hidden:
                default:
                    break;
            }
        }

        // ================================ INPUTS ================================
        private void OnToggleMap()
        {
            MapManager.Instance.MapState = MapState switch
            {
                MapState.Minimap => MapState.Fullscreen,
                MapState.Fullscreen => MapState.Minimap,
                _ => MapState.Fullscreen
            };
        }

        private void OnZoomInOut(InputValue value) =>
            MapManager.Instance.ZoomIn(Mathf.Clamp(value.Get<float>(), -1, 1) / 10f);

        private void OnDeselectAll()
        {
            if (MapState == MapState.Fullscreen && MarkersSelectedCount > 0)
                MarkerManager.Instance.DeselectAllMarkers();
        }
    }
}