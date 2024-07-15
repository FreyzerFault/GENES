using System.Collections.Generic;
using Core;
using DavidUtils.ExtensionMethods;
using Markers;
using Procrain.Core;
using Procrain.MapDisplay;
using UnityEngine;

namespace GENES.UI.MapUI
{
    public class MapRendererUI : MapDisplayInTexture
    {
        [SerializeField] protected RectTransform imageRectTransform;
        [SerializeField] protected RectTransform frameRectTransform;

        protected override void Start()
        {
            base.Start();
            
            imageRectTransform = image.rectTransform;
            frameRectTransform = imageRectTransform.parent.GetComponent<RectTransform>();
            
            if (MarkerManager.Instance != null)
            {
                MarkerManager.Instance.OnMarkerAdded += HandleMarkerAdded;
                MarkerManager.Instance.OnMarkerRemoved += HandleMarkerRemoved;
                MarkerManager.Instance.OnMarkersClear += HandleMarkerClear;
            }
            
            // Resize Image to apply Zoom
            ApplyZoomSmooth();

            // Render MARKERS
            UpdateMarkers();
        }

        protected virtual void OnEnable()
        {
            if (MapInputController.Instance != null)
                MapInputController.Instance.OnZoom += ZoomIn;
        }

        private void OnDisable()
        {
            if (MapInputController.Instance != null)
                MapInputController.Instance.OnZoom -= ZoomIn;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (MarkerManager.Instance == null) return;

            MarkerManager.Instance.OnMarkerAdded -= HandleMarkerAdded;
            MarkerManager.Instance.OnMarkerRemoved -= HandleMarkerRemoved;
            MarkerManager.Instance.OnMarkersClear -= HandleMarkerClear;
        }
        
        private void Update()
        {
            UpdatePlayerIcon();
            ApplyZoomSmooth();
        }

        #region BUILD MAP

        public override void DisplayMap()
        {
            base.DisplayMap();
            UpdatePlayerIcon();
            ApplyZoomSmooth();
        }

        #endregion
        
        
        #region MARKERS

        private readonly List<MarkerUI> _markersUIObjects = new();
        [SerializeField] private MarkerUI markerUIPrefab;
        [SerializeField] private Transform markersUIParent;
        
        private void HandleMarkerAdded(Marker marker, int index) => InstantiateMarker(marker, index);
        private void HandleMarkerRemoved(Marker marker, int index) => DestroyMarkerUI(index);
        private void HandleMarkerClear() => ClearMarkersUI();


        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (Marker marker in MarkerManager.Instance.Markers)
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
            MarkerUI markerUI = _markersUIObjects[index];

            if (Application.isPlaying) Destroy(markerUI.gameObject);
            else DestroyImmediate(markerUI.gameObject);

            _markersUIObjects.RemoveAt(index);
        }
        
        private void ClearMarkersUI()
        {
            foreach (MarkerUI markerUI in _markersUIObjects)
            {
                if (Application.isPlaying) Destroy(markerUI.gameObject);
                else DestroyImmediate(markerUI.gameObject);
            }
            _markersUIObjects.Clear();
        }

        public void ToggleMarkers(bool value) => markersUIParent.gameObject.SetActive(value);

        #endregion

        #region PLAYER ICON
        
        [SerializeField] private RectTransform playerSprite;

        public void UpdatePlayerIcon()
        {
            if (playerSprite == null) return;

            playerSprite.anchoredPosition = MapManager.Instance.PlayerNormalizedPosition * frameRectTransform.rect.size;
            playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            // Asignar el pivot a la posicion del jugador normalizada para que cada movimiento sea relativo a él
            imageRectTransform.pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Posicionar el mapa con el pivot en el centro del Frame
            Vector2 frameCenter = frameRectTransform.Corners().bottomLeft + frameRectTransform.Size() / 2;
            imageRectTransform.position = frameCenter;
        }

        #endregion
        
        #region Zoom
        
        [SerializeField] private float zoom = 1;
        [SerializeField] private float zoomChangeRate = 2;
        [Range(1.001f, 4f)] [SerializeField] private float zoomInScale = 1.5f;
        [Range(0.1f, 0.99f)] [SerializeField] private float zoomOutScale = 0.75f;
        private float _zoomTarget = 1;
        public float Zoom
        {
            get => zoom;
            set
            {
                zoom = value;
                ApplyZoomSmooth();
            }
        }
        
        // Add Zoom to the Map
        private void ZoomIn(float zoomScale)
        {
            // [-1,1] => [0, 1] => [0.75, 1.5]
            zoomScale = zoomScale / 2 + 0.5f;
            zoomScale = Mathf.Lerp(zoomOutScale, zoomInScale, zoomScale);

            _zoomTarget = Mathf.Max(1, zoom * zoomScale);
        }

        // Apply it Smoothly. Needs to be called in Update
        public void ApplyZoomSmooth()
        {
            float velRef = 0;
            zoom = Mathf.SmoothDamp(zoom, _zoomTarget, ref velRef, Time.deltaTime / zoomChangeRate);

            // Escalar el mapa relativo al centro
            imageRectTransform.localScale = new Vector3(zoom, zoom, 1);

            // Escalar el jugador a la inversa
            if (playerSprite != null)
                ScalePlayer();

            foreach (MarkerUI markerUI in _markersUIObjects)
                markerUI.UpdateScaleByZoom(zoom);

            StickToBorder();
        }

        // Inverse Scale Player to keep it same size in Screen
        private void ScalePlayer() => playerSprite.localScale = new Vector3(1 / zoom, 1 / zoom, 1);

        // If the border of the map is inside the Frame, it is readjusted so that it is not seen
        private void StickToBorder()
        {
            RectTransformExtensionMethods.RectCorners imgCorners = imageRectTransform.Corners();
            RectTransformExtensionMethods.RectCorners frameCorners = frameRectTransform.Corners();

            Vector3 distanceToLowerCorner = Vector3.Max(
                imgCorners.bottomLeft - frameCorners.bottomLeft,
                Vector3.zero
            );
            Vector3 distanceToUpperCorner = Vector3.Max(
                frameCorners.topRight - imgCorners.topRight,
                Vector3.zero
            );
            imageRectTransform.position += distanceToUpperCorner - distanceToLowerCorner;
        }

        #endregion

        #region DEBUG
       
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
