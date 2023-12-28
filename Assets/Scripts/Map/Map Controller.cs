using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private GameObject minimapParent;
        [SerializeField] private GameObject fullScreenMapParent;

        [SerializeField] private MapRenderer minimap;
        [SerializeField] private MapRenderer fullscreenMap;

        private void Awake()
        {
            minimap = minimapParent.GetComponentInChildren<MapRenderer>();
            fullscreenMap = fullScreenMapParent.GetComponentInChildren<MapRenderer>();
        }

        // INPUTS
        private void OnToggleMap()
        {
            if (minimapParent.activeSelf)
            {
                minimapParent.SetActive(false);
                fullScreenMapParent.SetActive(true);
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
            var zoom = value.Get<float>() / 10f;
            minimap.zoomScale += zoom;
        }
    }
}