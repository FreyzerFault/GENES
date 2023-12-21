using UnityEngine;
using UnityEngine.InputSystem;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject minimapParent;
    [SerializeField] private GameObject fullScreenMapParent;

    [SerializeField] private Minimap minimap;
    [SerializeField] private Minimap fullScreenMap;

    private void Awake()
    {
        minimap = minimapParent.GetComponentInChildren<Minimap>();
        fullScreenMap = fullScreenMapParent.GetComponentInChildren<Minimap>();
    }

    // INPUTS
    private void OnToggleMap(InputValue value)
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