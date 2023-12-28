using System;
using System.Collections.Generic;
using EditorCools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Gradient = UnityEngine.Gradient;

namespace Map
{
    public class MapRenderer : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler
    {
        [SerializeField] private HeightMap map;

        public float zoomScale = 2;
        public Gradient heightGradient = new();

        // MARKERS
        [SerializeField] private bool removeMarkersMode;

        [SerializeField] private MapMarkerGenerator markerGenerator;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private Transform parentUI;

        // LINE
        [SerializeField] private UILineRenderer lineRenderer;

        // Objetos que siguen al cursor
        [SerializeField] private RectTransform mouseCursorMarker;
        [SerializeField] private RectTransform mouseLabel;

        private RawImage _image;
        private RectTransform _playerSprite;


        private float ImageWidth => _image.rectTransform.rect.width;
        private float ImageHeight => _image.rectTransform.rect.height;
        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        private Vector2 OriginPoint
        {
            get { 
                Vector3[] corners = new Vector3[4];
                _image.rectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        private void Awake()
        {
            Initialize();
            RenderTerrain();
        }

        private void Update()
        {
            UpdatePlayerPoint();
            ZoomToPlayer();
        }

        // ================================== MOUSE EVENTS ==================================
        public void OnPointerClick(PointerEventData eventData)
        {
            var normalizedPosition = GetNormalizedPosition(eventData.position);

            if (removeMarkersMode)
            {
                var marker = markerGenerator.RemovePoint(normalizedPosition);
                if (marker != null) UpdateMarkersInUI();
            }
            else
            {
                // Añade o Selecciona un punto
                markerGenerator.AddPoint(normalizedPosition, out MapMarker marker, out _, out _, out bool collision);
                
                UpdateMarkersInUI();
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (mouseLabel != null) mouseLabel.position = eventData.position + new Vector2(0, 40);
            if (mouseCursorMarker != null)
                mouseCursorMarker.position = eventData.position;
        }


        private void Initialize()
        {
            map ??= FindObjectOfType<HeightMap>();
            _image ??= GetComponent<RawImage>();
            _playerSprite ??= GetComponentInChildren<Image>().rectTransform;
        }


        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            // Create Texture of Map
            _image.texture = map.GetTexture((int)ImageWidth, (int)ImageHeight, heightGradient);

            // Scale Texture with UV
            // var uvRect = _image.uvRect;
            // uvRect.width = map.TerrainWidth / ImageWidth;
            // uvRect.height = map.TerrainHeight / ImageHeight;
            // _image.uvRect = uvRect;
        }

        // ================================== PLAYER POINT ==================================
        private void UpdatePlayerPoint()
        {
            _playerSprite.anchoredPosition = map.PlayerNormalizedPosition * ImageSize;
            _playerSprite.rotation = map.PlayerRotationForUI;
        }


        private void ZoomToPlayer()
        {
            // Centrar el Player en el centro del minimapa por medio de su pivot
            Vector2 pivot = map.PlayerNormalizedPosition;

            // Tamaño del minimapa escalado y Normalizado
            var mapSizeScaled = ImageSize / zoomScale;

            // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
            var minDistanceNormalized = mapSizeScaled / 2 / ImageSize;
            if (map.PlayerDistanceToBotLeftBorder.x < minDistanceNormalized.x)
                pivot = new Vector2(minDistanceNormalized.x, pivot.y);
            if (map.PlayerDistanceToBotLeftBorder.y < minDistanceNormalized.y)
                pivot = new Vector2(pivot.x, minDistanceNormalized.y);
            if (map.PlayerDistanceToTopRightBorder.x < minDistanceNormalized.x)
                pivot = new Vector2(1 - minDistanceNormalized.x, pivot.y);
            if (map.PlayerDistanceToTopRightBorder.y < minDistanceNormalized.y)
                pivot = new Vector2(pivot.x, 1 - minDistanceNormalized.y);

            // Pivot reajustado
            _image.rectTransform.pivot = pivot;

            // Escalar el mapa relativo al centro donde está el Player
            _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            _playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);
        }


        // ================================== MARKERS ==================================
        public void ToggleMarkers(bool value)
        {
            parentUI.gameObject.SetActive(value);
        }

        public void SetRemoveMode(bool value)
        {
            removeMarkersMode = value;
        }

        public void ClearAllPoints()
        {
            foreach (Transform markerUI in parentUI) Destroy(markerUI.gameObject);
            List<Vector2> pointList = new List<Vector2>(lineRenderer.Points);
            pointList.Clear();
            lineRenderer.Points = pointList.ToArray();
            markerGenerator.ClearAllPoints();
        }

        // RENDER MARKERS
        private void UpdateMarkersInUI()
        {
            // Clear Markers UI
            foreach (Transform child in parentUI) Destroy(child.gameObject);

            Vector2 imageSize = GetComponent<RectTransform>().rect.size;
            for (int i = 0; i < markerGenerator.MarkerPoints.Count; i++)
            {
                MapMarker marker = markerGenerator.MarkerPoints[i];
                marker.labelText = $"{i} - {marker.worldPosition}";
                GameObject markerUIObj = Instantiate(markerPrefab, parentUI);
                markerUIObj.GetComponent<RectTransform>().localPosition = GetPointInMap(marker.normalizedPosition);
                marker.UpdateMarkerUI(markerUIObj);
            }

            UpdateLine();
        }

        // ================================== LINE RENDERER ==================================
        private void UpdateLine()
        {
            List<Vector2> pointList = new List<Vector2>(lineRenderer.Points);
            pointList.Clear();
            lineRenderer.Points = pointList.ToArray();
            
            markerGenerator.MarkerPoints.ForEach(marker =>
            {
                pointList.Add(GetPointInMap(marker.normalizedPosition));
            });
            
            lineRenderer.Points = pointList.ToArray();
        }
        
        // ================================== UTILS ==================================
        private Vector2 GetNormalizedPosition(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), screenPos,
                null, out Vector2 localPos);
            return (localPos + ImageSize / 2) / ImageSize;
        }

        private Vector2 GetPointInMap(Vector2 normalizedPos)
        {
            Vector2 imageSize = GetComponent<RectTransform>().rect.size;
            return (normalizedPos - new Vector2(0.5f, 0.5f)) * imageSize;
        }

        // ================================== BUTTONS ==================================
        [Button("Update Map")]
        private void UpdateMapButton()
        {
            Initialize();
            RenderTerrain();
            ZoomToPlayer();
            UpdatePlayerPoint();
        }

        // BUTTONS
        [Button("Update Player Point")]
        private void UpdatePlayerPointButton()
        {
            Initialize();
            UpdatePlayerPoint();
        }

        [Button("Zoom to PLayer")]
        private void ZoomToPlayerButton()
        {
            Initialize();
            ZoomToPlayer();
        }

        [Button("Render Terrain")]
        private void RenderTerrainButton()
        {
            Initialize();
            RenderTerrain();
        }
    }
}