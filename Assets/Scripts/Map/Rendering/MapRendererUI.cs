using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using MyBox;
using Procrain.MapGeneration.Texture;
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
        [SerializeField] private Image image;
        [SerializeField] protected RectTransform frameRectTransform;
        [SerializeField] protected RectTransform imageRectTransform;
        [SerializeField] private float zoom = 1;
        [SerializeField] private float zoomChangeRate = 2;
        [Range(1.001f, 4f)] [SerializeField] private float zoomInScale = 1.5f;
        [Range(0.1f, 0.99f)] [SerializeField] private float zoomOutScale = 0.75f;

        private readonly List<MarkerUI> _markersUIObjects = new();
        private float zoomTarget = 1;


        public float Zoom
        {
            get => zoom;
            set
            {
                zoom = value;
                UpdateZoom();
            }
        }

        private float ImageWidth =>
            image.rectTransform.rect.width * image.rectTransform.localScale.x;

        private float ImageHeight =>
            image.rectTransform.rect.height * image.rectTransform.localScale.y;

        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        // private float ZoomScale => MapManager.Instance.Zoom;

        protected MarkerManager MarkerManager => MarkerManager.Instance;

        private Vector2 OriginPoint
        {
            get
            {
                var corners = new Vector3[4];
                imageRectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        // ================================== UNITY ==================================


        private void Start()
        {
            imageRectTransform = image.GetComponent<RectTransform>();
            frameRectTransform = imageRectTransform.parent.GetComponent<RectTransform>();

            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded += HandleAdded;
            MarkerManager.OnMarkerRemoved += HandleRemoved;
            MarkerManager.OnMarkersClear += HandleClear;

            // RENDER
            RenderTerrain();
            UpdateZoom();

            // MARKERS
            UpdateMarkers();
        }

        private void Update()
        {
            UpdatePlayerPoint();
        }

        private void OnDestroy()
        {
            if (MarkerManager == null) return;

            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded -= HandleAdded;
            MarkerManager.OnMarkerRemoved -= HandleRemoved;
            MarkerManager.OnMarkersClear -= HandleClear;
        }

        // ============================= DEBUG =============================

        // private void OnDrawGizmos()
        // {
        //     // IMAGE
        //     var imagePos = imageRectTransform.PivotGlobal();
        //     var imageBotLeft = imageRectTransform.Corners().BottomLeft;
        //     var imageTopRight = imageRectTransform.Corners().TopRight;
        //
        //     var corners = new Vector3[4];
        //     imageRectTransform.GetWorldCorners(corners);
        //
        //     // MIN y MAX
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawSphere(imagePos, 10);
        //
        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(imageBotLeft, 10);
        //     Gizmos.DrawSphere(imageTopRight, 10);
        //
        //     Gizmos.color = Color.gray;
        //     Gizmos.DrawLine(imageBotLeft, imageTopRight);
        // }

        // ================================== EVENT SUSCRIBERS ==================================
        private void HandleAdded(Marker marker, int index) => InstantiateMarker(marker, index);

        private void HandleRemoved(Marker marker, int index) => DestroyMarkerUI(index);

        private void HandleClear() => ClearMarkersUI();

        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            var heightMap = MapManager.Instance.heightMap;
            // Create Texture of Map
            var texture = TextureGenerator.BuildTexture2D(heightMap, heightGradient);

            texture.Apply();
            image.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, heightMap.Size, heightMap.Size),
                Vector2.one / 2
            );
        }

        // ================================== PLAYER POINT ==================================
        private void UpdatePlayerPoint()
        {
            playerSprite.anchoredPosition =
                MapManager.Instance.PlayerNormalizedPosition * frameRectTransform.rect.size;
            playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            UpdateZoom();
        }

        public void ZoomIn(float zoomScale)
        {
            // [-1,1] => [0, 1] => [0.75, 1.5]
            zoomScale = zoomScale / 2 + 0.5f;
            zoomScale = Mathf.Lerp(zoomOutScale, zoomInScale, zoomScale);

            zoomTarget = Mathf.Max(1, zoom * zoomScale);
        }

        private void UpdateZoom()
        {
            float velRef = 0;
            zoom = Mathf.SmoothDamp(zoom, zoomTarget, ref velRef, Time.deltaTime / zoomChangeRate);

            // Asignar el pivot a la posicion del jugador normalizada para que cada movimiento sea relativo a él
            image.rectTransform.pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Posicionar el mapa en el centro del frame
            var frameCenter =
                frameRectTransform.Corners().BottomLeft + frameRectTransform.Diagonal() / 2;
            // var frameCenter = frameRectTransform.TransformPoint(frameRectTransform.rect.size / 2);
            imageRectTransform.position = frameCenter;

            // Escalar el mapa relativo al centro donde está el Player
            image.rectTransform.localScale = new Vector3(zoom, zoom, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            playerSprite.localScale = new Vector3(1 / zoom, 1 / zoom, 1);

            // Reajustar el offset hacia los bordes para que no se vea el fondo
            var imgCorners = new Vector3[4];
            var frameCorners = new Vector3[4];
            imageRectTransform.GetWorldCorners(imgCorners);
            frameRectTransform.GetWorldCorners(frameCorners);

            var distanceToLowerCorner = Vector3.Max(imgCorners[0] - frameCorners[0], Vector3.zero);
            var distanceToUpperCorner = Vector3.Max(frameCorners[2] - imgCorners[2], Vector3.zero);
            image.rectTransform.position += distanceToUpperCorner - distanceToLowerCorner;
        }

        // ================================== MARKERS ==================================
        private void ClearMarkersUI()
        {
            GetComponentsInChildren<MarkerUI>()
                .ToList()
                .ForEach(
                    marker =>
                    {
                        if (Application.isPlaying)
                            Destroy(marker.gameObject);
                        else
                            DestroyImmediate(marker.gameObject);
                    }
                );
        }

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (Marker marker in MarkerManager.Markers) InstantiateMarker(marker);
        }

        private void InstantiateMarker(Marker marker, int index = -1)
        {
            var markerUI = Instantiate(markerUIPrefab, markersUIParent).GetComponent<MarkerUI>();
            markerUI.marker = marker;
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
            UpdateZoom();
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
            UpdateZoom();
        }

        [ButtonMethod]
        protected void ReRenderTerrain()
        {
            RenderTerrain();
        }
#endif
    }
}