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
        public Gradient heightGradient = new();

        // Icono del Player
        [SerializeField] private RectTransform playerSprite;

        // MARKERS
        [SerializeField] private Transform markersUIParent;
        [SerializeField] private MarkerUI markerUIPrefab;

        // Image
        [SerializeField] private RawImage image;
        private readonly List<MarkerUI> _markersUIObjects = new();
        protected RectTransform FrameRectTransform;
        protected RectTransform ImageRectTransform;
        private float ImageWidth => image.rectTransform.rect.width * image.rectTransform.localScale.x;
        private float ImageHeight => image.rectTransform.rect.height * image.rectTransform.localScale.y;
        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        private float ZoomScale => MapManager.Instance.Zoom;

        protected MarkerManager MarkerManager => MarkerManager.Instance;


        private Vector2 OriginPoint
        {
            get
            {
                var corners = new Vector3[4];
                ImageRectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        // ================================== UNITY ==================================

        private void Start()
        {
            ImageRectTransform = image.GetComponent<RectTransform>();
            FrameRectTransform = ImageRectTransform.parent.GetComponent<RectTransform>();

            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded += HandleAdded;
            MarkerManager.OnMarkerRemoved += HandleRemoved;
            MarkerManager.OnMarkersClear += HandleClear;
            MapManager.Instance.OnZoomChanged += HandleZoomChange;

            // RENDER
            RenderTerrain();
            Zoom(ZoomScale);

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
            MapManager.Instance.OnZoomChanged -= HandleZoomChange;
        }

        private void OnDrawGizmos()
        {
            var playerLocalPos = FrameRectTransform.InverseTransformPoint(playerSprite.position);
            var frameSize = FrameRectTransform.rect.size;
            var frameCenter = frameSize / 2;
            var displacement = frameCenter + (Vector2)playerLocalPos;
            var mapSizeDif = ImageSize - frameSize;

            displacement.x = Mathf.Clamp(displacement.x, -mapSizeDif.x, mapSizeDif.x);
            displacement.y = Mathf.Clamp(displacement.y, -mapSizeDif.y, mapSizeDif.y);

            Gizmos.color = Color.white;
            Gizmos.DrawRay(FrameRectTransform.position + playerLocalPos, displacement);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(FrameRectTransform.position + playerLocalPos, frameCenter - (Vector2)playerLocalPos);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(FrameRectTransform.position + playerLocalPos, mapSizeDif);


            RectTransformUtility.ScreenPointToLocalPointInRectangle(FrameRectTransform,
                Input.mousePosition, null, out var localMousePosition);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(FrameRectTransform.position + playerLocalPos, 20);
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
            playerSprite.anchoredPosition = MapManager.Instance.PlayerNormalizedPosition * FrameRectTransform.rect.size;
            playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            Zoom(ZoomScale);
        }

        private void HandleZoomChange(float zoom)
        {
            Zoom(zoom);
        }

        private void Zoom(float zoom)
        {
            // Posicion y Escalado relativos al jugador
            image.rectTransform.pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Posicionar el mapa en el centro del frame
            var newPosition = FrameRectTransform.TransformPoint(FrameRectTransform.rect.size / 2);
            var distanceToLowerCorner = (Vector3)ImageRectTransform.rect.min - FrameRectTransform.position;
            distanceToLowerCorner.x = Mathf.Max(distanceToLowerCorner.x, 0);
            distanceToLowerCorner.y = Mathf.Max(distanceToLowerCorner.y, 0);
            newPosition -= distanceToLowerCorner;

            ImageRectTransform.position = newPosition;


            // Escalar el mapa relativo al centro donde está el Player
            image.rectTransform.localScale = new Vector3(zoom, zoom, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            playerSprite.localScale = new Vector3(1 / zoom, 1 / zoom, 1);
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
            Zoom(ZoomScale);
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
            Zoom(ZoomScale);
        }

        [ButtonMethod]
        protected void ReRenderTerrain()
        {
            RenderTerrain();
        }
#endif
    }
}