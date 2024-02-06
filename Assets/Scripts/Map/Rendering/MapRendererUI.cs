using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using MyBox;
using UnityEngine;
using UnityEngine.UI;
using Gradient = UnityEngine.Gradient;

namespace Map.Rendering
{
    public class MapRendererUI : MonoBehaviour
    {
        [SerializeField] private float zoomScale = 2;

        public Gradient heightGradient = new();

        // PATH Renderer
        [SerializeField] private PathRendererUI pathRenderer;
        [SerializeField] private int lineDefaultThickness = 7;

        // Icono del Player
        [SerializeField] private RectTransform playerSprite;

        // MARKERS
        [SerializeField] private Transform markersUIParent;
        [SerializeField] private MarkerUI markerUIPrefab;

        [SerializeField] private RawImage image;

        [SerializeField] protected RectTransform RectTransform;
        private readonly List<MarkerUI> _markersUIObjects = new();

        protected MarkerManager MarkerManager => MarkerManager.Instance;

        public float ZoomScale
        {
            get => zoomScale;
            set
            {
                zoomScale = value;
                Zoom();
            }
        }

        private float ImageWidth => image.rectTransform.rect.width;
        private float ImageHeight => image.rectTransform.rect.height;
        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        private Vector2 OriginPoint
        {
            get
            {
                var corners = new Vector3[4];
                image.rectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        // ================================== UNITY ==================================
        private void Awake()
        {
            image = GetComponent<RawImage>();
            pathRenderer = GetComponentInChildren<PathRendererUI>();
        }

        private void Start()
        {
            RectTransform = GetComponent<RectTransform>();

            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded += HandleAdded;
            MarkerManager.OnMarkerRemoved += HandleRemoved;
            MarkerManager.OnMarkersClear += HandleClear;

            // RENDER
            RenderTerrain();
            Zoom();

            // MARKERS
            UpdateMarkers();
        }

        private void Update()
        {
            UpdatePlayerPoint();
        }

        private void OnDestroy()
        {
            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded -= HandleAdded;
            MarkerManager.OnMarkerRemoved -= HandleRemoved;
            MarkerManager.OnMarkersClear -= HandleClear;
        }


        // ================================== EVENT SUSCRIBERS ==================================
        private void HandleAdded(Marker marker, int index) => InstantiateMarker(marker, index);
        private void HandleRemoved(Marker marker, int index) => DestroyMarkerUI(index);
        private void HandleClear() => ClearMarkersUI();

        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            // Create Texture of Map
            image.texture =
                MapManager.Terrain.ToTexture((int)ImageWidth, (int)ImageHeight, heightGradient);
        }

        // ================================== PLAYER POINT ==================================
        private void UpdatePlayerPoint()
        {
            playerSprite.anchoredPosition = MapManager.Instance.PlayerNormalizedPosition * ImageSize;
            playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            CenterPlayerInZoomedMap();
        }

        private void CenterPlayerInZoomedMap()
        {
            if (Math.Abs(zoomScale - 1) < 0.01f) return;

            // Centrar el Player en el centro del minimapa por medio de su pivot
            var pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Tamaño del minimapa escalado y Normalizado
            var mapSizeScaled = ImageSize * zoomScale;
            var displacement = ImageSize / 3;

            // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
            var distanceToBotLeft = MapManager.Instance.PlayerNormalizedPosition * mapSizeScaled;
            displacement.x = Mathf.Min(distanceToBotLeft.x, displacement.x);
            displacement.y = Mathf.Min(distanceToBotLeft.y, displacement.y);

            // Pivot reajustado
            image.rectTransform.pivot = pivot;
            image.rectTransform.anchoredPosition = displacement;
        }

        private void Zoom()
        {
            // Escalar el mapa relativo al centro donde está el Player
            image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);

            // Line Thickness
            pathRenderer.LineThickness = lineDefaultThickness / zoomScale;
        }

        // ================================== MARKERS ==================================
        private void ClearMarkersUI()
        {
            GetComponentsInChildren<MarkerUI>().ToList().ForEach(marker =>
            {
                if (Application.isPlaying)
                    Destroy(marker.gameObject);
                else
                    DestroyImmediate(marker.gameObject);
            });
        }

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (var marker in MarkerManager.Markers) InstantiateMarker(marker);
        }

        private void InstantiateMarker(Marker marker, int index = -1)
        {
            var markerUI = Instantiate(markerUIPrefab, markersUIParent).GetComponent<MarkerUI>();
            markerUI.Marker = marker;
            _markersUIObjects.Insert(index == -1 ? _markersUIObjects.Count : index, markerUI);
        }

        private void DestroyMarkerUI(int index)
        {
            var markerUI = _markersUIObjects[index];

            if (Application.isPlaying)
                Destroy(markerUI.gameObject);
            else
                DestroyImmediate(markerUI.gameObject);

            _markersUIObjects.RemoveAt(index);
        }

        public void ToggleMarkers(bool value)
        {
            markersUIParent.gameObject.SetActive(value);
        }

#if UNITY_EDITOR
        // ================================== BUTTONS on INSPECTOR ==================================
        [ButtonMethod]
        protected void UpdateMap()
        {
            RenderTerrain();
            Zoom();
            UpdatePlayerPoint();
        }

        // BUTTONS
        [ButtonMethod]
        protected void UpdatePlayerPointInMap()
        {
            UpdatePlayerPoint();
        }

        [ButtonMethod]
        protected void ZoomMapToPlayerPosition()
        {
            Zoom();
        }

        [ButtonMethod]
        protected void ReRenderTerrain()
        {
            RenderTerrain();
        }
#endif
    }
}