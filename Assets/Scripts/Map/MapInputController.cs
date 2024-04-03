using Core;
using Map.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapInputController : MonoBehaviour
    {
        [SerializeField] private MapRendererUI minimap;

        [SerializeField] private MapRendererUI fullScreenMap;

        [SerializeField] private GameObject MinimapParent => minimap.transform.parent.gameObject;

        [SerializeField] private GameObject FullScreenMapParent => fullScreenMap.transform.parent.gameObject;

        private MapState MapState => MapManager.Instance.MapState;
        private int MarkersSelectedCount => MarkerManager.Instance.SelectedCount;

        private void Awake()
        {
            HandleStateChanged(MapState);
            MapManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void Update()
        {
            // SHIFT => Remove Mode
            var shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift);
            MarkerManager.Instance.EditMarkerMode = shiftPressed
                ? EditMarkerMode.Delete
                : EditMarkerMode.Add;
        }

        private void OnDestroy()
        {
            MapManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(MapState state)
        {
            if (FullScreenMapParent == null || MinimapParent == null) return;
            FullScreenMapParent.SetActive(false);
            MinimapParent.SetActive(false);

            switch (state)
            {
                case MapState.Fullscreen:
                    FullScreenMapParent.SetActive(true);
                    break;
                case MapState.Minimap:
                    MinimapParent.SetActive(true);
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

            GameManager.Instance.State =
                MapManager.Instance.MapState == MapState.Fullscreen
                    ? GameManager.GameState.Paused
                    : GameManager.GameState.Playing;
        }

        private void OnZoomInOut(InputValue value)
        {
            var zoomScale = Mathf.Clamp(value.Get<float>(), -1, 1);
            if (zoomScale == 0) return;

            switch (MapManager.Instance.MapState)
            {
                case MapState.Minimap:
                    minimap.ZoomIn(zoomScale);
                    break;
                case MapState.Fullscreen:
                    fullScreenMap.ZoomIn(zoomScale);
                    break;
            }
        }

        private void OnDeselectAll()
        {
            if (MapState == MapState.Fullscreen && MarkersSelectedCount > 0)
                MarkerManager.Instance.DeselectAllMarkers();
        }
    }
}