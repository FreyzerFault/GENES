using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Map
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private GameObject minimapParent;
        [SerializeField] private GameObject fullScreenMapParent;

        [SerializeField] private MapUIRenderer minimap;

        [FormerlySerializedAs("fullscreenMap")] [SerializeField]
        private MapUIRenderer fullscreenMapUI;

        private void Awake()
        {
            minimap = minimapParent.GetComponentInChildren<MapUIRenderer>();
            fullscreenMapUI = fullScreenMapParent.GetComponentInChildren<MapUIRenderer>();
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
            minimap.ZoomScale += value.Get<float>() / 10f;
        }
    }
}