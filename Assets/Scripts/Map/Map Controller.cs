using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private GameObject minimapParent;
        [SerializeField] private GameObject fullScreenMapParent;

        [SerializeField] private MapUIRenderer minimapUI;
        [SerializeField] private MapUIRenderer fullscreenMapUI;

        private MapUIRenderer Minimap =>
            minimapUI ? minimapUI : GameObject.FindWithTag("Minimap").GetComponent<MapUIRenderer>();

        private MapUIRenderer FullScreenMap => fullscreenMapUI
            ? fullscreenMapUI
            : GameObject.FindWithTag("Map Fullscreen").GetComponent<MapUIRenderer>();


        private void Awake()
        {
            minimapUI = GameObject.FindWithTag("Minimap")?.GetComponent<MapUIRenderer>();
            fullscreenMapUI = GameObject.FindWithTag("Map Fullscreen")?.GetComponent<MapUIRenderer>();
        }

        // INPUTS
        private void OnToggleMap()
        {
            if (minimapParent.activeSelf)
            {
                minimapParent.SetActive(false);
                fullScreenMapParent.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (fullScreenMapParent.activeSelf)
            {
                fullScreenMapParent.SetActive(false);
                minimapParent.SetActive(true);
            }
            else
            {
                minimapParent.SetActive(true);
            }
        }

        private void OnZoomInOut(InputValue value)
        {
            if (minimapParent.activeSelf)
                Minimap.ZoomScale += Mathf.Clamp(value.Get<float>(), -1, 1) / 10f;
            else if (fullScreenMapParent.activeSelf)
                fullscreenMapUI.ZoomScale += Mathf.Clamp(value.Get<float>(), -1, 1) / 10f;
        }
    }
}