using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using MyBox;
using Procrain.MapGeneration;
using UnityEngine;
using UnityEngine.UI;
using Gradient = UnityEngine.Gradient;

namespace Map.Rendering
{
    public class MapRendererUI : MonoBehaviour
    {
        protected MapManager MapManager => MapManager.Instance;
        protected IHeightMap HeightMap => MapManager.HeightMap;
        protected Gradient HeightGradient => MapManager.heightGradient;

        // MARKERS
        protected MarkerManager MarkerManager => MarkerManager.Instance;

        [SerializeField]
        private Transform markersUIParent;

        [SerializeField]
        private MarkerUI markerUIPrefab;

        // Icono del Player
        [SerializeField]
        private RectTransform playerSprite;

        // Image
        [SerializeField]
        private Image image;

        [SerializeField]
        protected RectTransform imageRectTransform;

        [SerializeField]
        protected RectTransform frameRectTransform;

        [SerializeField]
        private float zoom = 1;

        [SerializeField]
        private float zoomChangeRate = 2;

        [Range(1.001f, 4f)]
        [SerializeField]
        private float zoomInScale = 1.5f;

        [Range(0.1f, 0.99f)]
        [SerializeField]
        private float zoomOutScale = 0.75f;

        private readonly List<MarkerUI> _markersUIObjects = new();
        private float zoomTarget = 1;

        public float Zoom
        {
            get => zoom;
            set
            {
                zoom = value;
                ApplyZoomSmooth();
            }
        }

        private Vector2 ImageSize => image.rectTransform.Size();
        private float ImageWidth => ImageSize.x;
        private float ImageHeight => ImageSize.y;

        private float ZoomScale => MapManager.Instance.Zoom;

        // ================================== UNITY ==================================

        private void Start()
        {
            imageRectTransform = image.GetComponent<RectTransform>();
            frameRectTransform = imageRectTransform.parent.GetComponent<RectTransform>();

            // SUBSCRIBERS:
            MapManager.OnMapUpdated += RenderTerrain;
            if (MarkerManager != null)
            {
                MarkerManager.OnMarkerAdded += HandleMarkerAdded;
                MarkerManager.OnMarkerRemoved += HandleMarkerRemoved;
                MarkerManager.OnMarkersClear += HandleMarkerClear;
            }

            // Resize Image to apply Zoom
            ApplyZoomSmooth();

            // Render MARKERS
            UpdateMarkers();
        }

        private void Update() => UpdatePlayerIcon();

        private void OnDestroy()
        {
            MapManager.OnMapUpdated -= RenderTerrain;

            if (MarkerManager == null)
                return;

            // UNSUSCRIBE
            MarkerManager.OnMarkerAdded -= HandleMarkerAdded;
            MarkerManager.OnMarkerRemoved -= HandleMarkerRemoved;
            MarkerManager.OnMarkersClear -= HandleMarkerClear;
        }

        #region MARKERS

        private void HandleMarkerAdded(Marker marker, int index) =>
            InstantiateMarker(marker, index);

        private void HandleMarkerRemoved(Marker marker, int index) => DestroyMarkerUI(index);

        private void HandleMarkerClear() => ClearMarkersUI();

        private void ClearMarkersUI() =>
            GetComponentsInChildren<MarkerUI>()
                .ToList()
                .ForEach(marker =>
                {
                    if (Application.isPlaying)
                        Destroy(marker.gameObject);
                    else
                        DestroyImmediate(marker.gameObject);
                });

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (var marker in MarkerManager.Markers)
                InstantiateMarker(marker);
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

        public void ToggleMarkers(bool value) => markersUIParent.gameObject.SetActive(value);

        #endregion

        #region MAP RENDERING

        private Texture2D Texture => MapManager.Instance.texture;

        private void RenderTerrain(IHeightMap heightMap)
        {
            if (MapManager == null)
                return;

            // Create Texture of Map
            if (Texture == null)
                MapManager.BuildTexture2D_Sequential();

            Texture.Apply();
            image.sprite = Sprite.Create(
                Texture,
                new Rect(0, 0, heightMap.Size, heightMap.Size),
                Vector2.one / 2
            );
        }

        private void UpdatePlayerIcon()
        {
            if (playerSprite == null)
                return;

            playerSprite.anchoredPosition =
                MapManager.PlayerNormalizedPosition * frameRectTransform.rect.size;
            playerSprite.rotation = MapManager.PlayerRotationForUI;

            // Asignar el pivot a la posicion del jugador normalizada para que cada movimiento sea relativo a él
            image.rectTransform.pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Posicionar el mapa con el pivot en el centro del Frame
            var frameCenter =
                frameRectTransform.Corners().bottomLeft + frameRectTransform.Size() / 2;
            imageRectTransform.position = frameCenter;

            ApplyZoomSmooth();
        }

        #endregion

        #region Zoom

        // Add Zoom to the Map
        public void ZoomIn(float zoomScale)
        {
            // [-1,1] => [0, 1] => [0.75, 1.5]
            zoomScale = zoomScale / 2 + 0.5f;
            zoomScale = Mathf.Lerp(zoomOutScale, zoomInScale, zoomScale);

            zoomTarget = Mathf.Max(1, zoom * zoomScale);
        }

        // Apply it Smoothly. Needs to be called in Update
        private void ApplyZoomSmooth()
        {
            float velRef = 0;
            zoom = Mathf.SmoothDamp(zoom, zoomTarget, ref velRef, Time.deltaTime / zoomChangeRate);

            // Escalar el mapa relativo al centro
            image.rectTransform.localScale = new Vector3(zoom, zoom, 1);

            // Escalar el jugador a la inversa
            if (playerSprite != null)
                ScalePlayer();

            StickToBorder();
        }

        // Inverse Scale Player to keep it same size in Screen
        private void ScalePlayer() => playerSprite.localScale = new Vector3(1 / zoom, 1 / zoom, 1);

        // If the border of the map is inside the Frame, it is readjusted so that it is not seen
        private void StickToBorder()
        {
            var imgCorners = imageRectTransform.Corners();
            var frameCorners = frameRectTransform.Corners();

            var distanceToLowerCorner = Vector3.Max(
                imgCorners.bottomLeft - frameCorners.bottomLeft,
                Vector3.zero
            );
            var distanceToUpperCorner = Vector3.Max(
                frameCorners.topRight - imgCorners.topRight,
                Vector3.zero
            );
            image.rectTransform.position += distanceToUpperCorner - distanceToLowerCorner;
        }

        #endregion

        #region DEBUG

#if UNITY_EDITOR
        // ================================== BUTTONS on INSPECTOR ==================================
        [ButtonMethod]
        protected void UpdateMap()
        {
            ReRenderTerrain();
            UpdatePlayerIcon();
            ZoomMapToPlayerPosition();
        }

        [ButtonMethod]
        protected void UpdatePlayerPointInMap() => UpdatePlayerIcon();

        [ButtonMethod]
        protected void ZoomMapToPlayerPosition() => ApplyZoomSmooth();

        [ButtonMethod]
        protected void ReRenderTerrain() => RenderTerrain(MapManager.HeightMap);
#endif
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

        #endregion
    }
}
