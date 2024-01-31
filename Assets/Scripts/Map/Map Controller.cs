using System;
using Map.Markers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Map
{
    public class MapInputController : MonoBehaviour
    {
        [SerializeField] private GameObject minimapParent;
        [SerializeField] private GameObject fullScreenMapParent;

        [SerializeField] private MapUIRenderer minimapUI;
        [SerializeField] private MapUIRenderer fullscreenMapUI;

        private MapUIRenderer Minimap =>
            minimapUI != null ? minimapUI : GameObject.FindWithTag("Minimap").GetComponent<MapUIRenderer>();

        private MapUIRenderer FullScreenMap => fullscreenMapUI != null
            ? fullscreenMapUI
            : GameObject.FindWithTag("Map Fullscreen").GetComponent<MapUIRenderer>();


        private void Awake()
        {
            minimapUI ??= GameObject.FindWithTag("Minimap")?.GetComponent<MapUIRenderer>();
            fullscreenMapUI ??= GameObject.FindWithTag("Map Fullscreen")?.GetComponent<MapUIRenderer>();
        }

        private void Start()
        {
            GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
            HandleGameStateChanged(GameManager.Instance.State);
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.Playing:
                    CloseMap();
                    break;
                case GameManager.GameState.Paused:
                    OpenMap();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void CloseMap()
        {
            fullScreenMapParent.SetActive(false);
            minimapParent.SetActive(true);
        }

        private void OpenMap()
        {
            minimapParent.SetActive(false);
            fullScreenMapParent.SetActive(true);
        }

        // INPUTS
        private void OnToggleMap()
        {
            if (GameManager.Instance.IsPlaying)
            {
                GameManager.Instance.State = GameManager.GameState.Paused;
            }
            else if (GameManager.Instance.IsPaused)
            {
                if (MarkerManager.Instance.SelectedCount > 0)
                    MarkerManager.Instance.DeselectAllMarkers();
                else
                    GameManager.Instance.State = GameManager.GameState.Playing;
            }
        }

        private void OnZoomInOut(InputValue value)
        {
            if (minimapParent.activeSelf)
                Minimap.ZoomScale += Mathf.Clamp(value.Get<float>(), -1, 1) / 10f;
        }

        private void OnDeselectAll()
        {
            if (GameManager.Instance.State == GameManager.GameState.Paused)
                MarkerManager.Instance.DeselectAllMarkers();
        }
    }
}