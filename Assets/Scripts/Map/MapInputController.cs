using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapInputController : MonoBehaviour
    {
        private static MapState MapState => MapManager.Instance.MapState;
        private static int MarkersSelectedCount => MarkerManager.Instance.SelectedCount;

        private void Update()
        {
            // SHIFT => Remove Mode
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift);
            MarkerManager.Instance.EditMarkerMode = shiftPressed
                ? EditMarkerMode.Delete
                : EditMarkerMode.Add;
        }


        // ================================ INPUTS ================================
        private void OnToggleMap()
        {
            MapManager.Instance.ToggleMap();
        }

        private void OnZoomInOut(InputValue value)
        {
            var zoomScale = Mathf.Clamp(value.Get<float>(), -1, 1);
            if (zoomScale == 0) return;

            MapManager.Instance.ZoomInOut(zoomScale);
        }

        private void OnDeselectAll()
        {
            if (MapState == MapState.Fullscreen && MarkersSelectedCount > 0)
                MarkerManager.Instance.DeselectAllMarkers();
        }
    }
}