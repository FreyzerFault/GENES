using System;
using System.Collections.Generic;
using System.Linq;
using EditorCools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Gradient = UnityEngine.Gradient;

namespace Map
{
    public class MapRenderer : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler
    {
        [SerializeField] private float zoomScale = 2;
        public float ZoomScale
        {
            get => zoomScale;
            set  {
                zoomScale = value;
                OnZoom();
            }
        }
        
        public Gradient heightGradient = new();

        // MARKERS
        [SerializeField] private MapMarkerManagerSO markerManager;
        [Serializable]enum MarkerMode { Add, Remove, Select, None }
        [SerializeField] private MarkerMode markerMode = MarkerMode.None;
        public bool RemoveMarkersMode
        {
            get => markerMode == MarkerMode.Remove;
            set => markerMode = value ? MarkerMode.Remove : MarkerMode.Add;
        }

        [SerializeField] private Transform mapMarkersParent;

        // LINE
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private int lineDefaultThickness = 7;

        // Objetos que siguen al cursor
        [SerializeField] private RectTransform mouseCursorMarker;
        [SerializeField] private RectTransform mouseLabel;

        private RawImage _image;
        [SerializeField] private RectTransform _playerSprite;


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

        // ================================== UNITY ==================================
        private void Start()
        {
            Initialize();
            RenderTerrain();
            OnZoom();
        }

        private void Update()
        {
            UpdatePlayerPoint();
        }

        // ================================== MOUSE EVENTS ==================================
        public void OnPointerClick(PointerEventData eventData)
        {
            if (markerMode == MarkerMode.None) return;
            
            // Posicion de 0 a 1
            var normalizedPosition = GetNormalizedPosition(eventData.position);

            switch (markerMode)
            {
                case MarkerMode.Add:
                    markerManager.AddPoint(normalizedPosition, out MapMarkerData _, out bool _);
                    break;
                case MarkerMode.Remove:
                    markerManager.RemovePoint(normalizedPosition);
                    break;
                case MarkerMode.Select:
                    // TODO - Seleccionar punto
                    break;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mouseLabel != null)
                mouseLabel.gameObject.SetActive(true);
            if (mouseCursorMarker != null)
                mouseCursorMarker.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (mouseLabel != null)
                mouseLabel.gameObject.SetActive(false);
            if (mouseCursorMarker != null)
                mouseCursorMarker.gameObject.SetActive(false);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (mouseLabel != null) mouseLabel.position = eventData.position + new Vector2(0, 40);
            if (mouseCursorMarker != null)
                mouseCursorMarker.position = eventData.position;
        }
        
        // ================================== INITIALIZATION ==================================
        
        private void Initialize()
        {
            _image ??= GetComponent<RawImage>();
            _playerSprite ??= GetComponentInChildren<Image>().rectTransform;
            
            
            // MARKERS
            UpdateMarkers();
            markerManager.OnMarkerAdded.AddListener((_) => UpdateMarkers());
            markerManager.OnMarkerRemoved.AddListener((_) => UpdateMarkers());
            markerManager.OnMarkersClear.AddListener(UpdateMarkers);
        }

        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            // Create Texture of Map
            _image.texture = MapManager.Instance.GetTexture((int)ImageWidth, (int)ImageHeight, heightGradient);

            // Scale Texture with UV
            // var uvRect = _image.uvRect;
            // uvRect.width = map.TerrainWidth / ImageWidth;
            // uvRect.height = map.TerrainHeight / ImageHeight;
            // _image.uvRect = uvRect;
        }

        // ================================== PLAYER POINT ==================================
        private void UpdatePlayerPoint()
        {
            _playerSprite.anchoredPosition = MapManager.Instance.PlayerNormalizedPosition * ImageSize;
            _playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            if (zoomScale != 1) CenterPlayerInZoomedMap();
        }

        private void CenterPlayerInZoomedMap()
        {
            // Centrar el Player en el centro del minimapa por medio de su pivot
            Vector2 pivot = MapManager.Instance.PlayerNormalizedPosition;
            
            // Tamaño del minimapa escalado y Normalizado
            var mapSizeScaled = ImageSize * zoomScale;
            Vector2 displacement = ImageSize / 2;
            
            // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
            Vector2 distanceToBotLeft = MapManager.Instance.PlayerDistanceToBotLeftBorder * mapSizeScaled;
            displacement.x = Mathf.Min(distanceToBotLeft.x, displacement.x);
            displacement.y = Mathf.Min(distanceToBotLeft.y, displacement.y);
            
            // var displacement = new Vector2(Mathf.Max(minDistanceNormalized.x, ImageSize.x / 2),
            //     Mathf.Max(minDistanceNormalized.y, ImageSize.y / 2));
            // if (MapManager.Instance.PlayerDistanceToBotLeftBorder.x < minDistanceNormalized.x)
            //     pivot = new Vector2(minDistanceNormalized.x, pivot.y);
            // if (MapManager.Instance.PlayerDistanceToBotLeftBorder.y < minDistanceNormalized.y)
            //     pivot = new Vector2(pivot.x, minDistanceNormalized.y);
            // if (MapManager.Instance.PlayerDistanceToTopRightBorder.x < minDistanceNormalized.x)
            //     pivot = new Vector2(1 - minDistanceNormalized.x, pivot.y);
            // if (MapManager.Instance.PlayerDistanceToTopRightBorder.y < minDistanceNormalized.y)
            //     pivot = new Vector2(pivot.x, 1 - minDistanceNormalized.y);
            
            // Pivot reajustado
            _image.rectTransform.pivot = pivot;
            _image.rectTransform.anchoredPosition = displacement;
        }
        
        public void OnZoom()
        {
            // Escalar el mapa relativo al centro donde está el Player
            _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            _playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);
            
            UpdateMarkers();
        }


        // ================================== MARKERS ==================================
        
        private void ClearMarkers() => GetComponentsInChildren<MapMarker>().ToList().ForEach(marker => Destroy(marker.gameObject));

        public void UpdateMarkers()
        {
            // Clear Markers UI
            ClearMarkers();

            // Se instancian de nuevo
            // Y se actualizan sus etiquetas en orden
            for (int i = 0; i < markerManager.MarkersCount; i++)
            {
                MapMarkerData marker = markerManager.Markers[i];
                marker.labelText = $"{i} - {marker.worldPosition}";
                InstantiateMarker(marker);
            }

            UpdateLine();
        }

        private void InstantiateMarker(MapMarkerData markerData)
        {
            MapMarker marker = Instantiate(markerManager.markerPrefab, mapMarkersParent).GetComponent<MapMarker>();
            marker.SetData(markerData);
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            // marker.GetComponent<RectTransform>().localPosition = GetLocalPointInMap(markerData.normalizedPosition);
            markerRect.anchorMax = new Vector2(0, 0);
            markerRect.anchorMin = new Vector2(0, 0);
            // markerRect.pivot = markerData.normalizedPosition;
            markerRect.anchoredPosition = GetLocalPointInMap(markerData.normalizedPosition);
            
            markerRect.localScale /= zoomScale;
        }
        
        public void ToggleMarkers(bool value) => mapMarkersParent.gameObject.SetActive(value);


        // ================================== LINE RENDERER ==================================
        private void UpdateLine()
        {
            lineRenderer.Points = markerManager.Markers.ToList().ConvertAll(
                    marker => GetLocalPointInMap(marker.normalizedPosition)
                ).ToArray();
            
            // Thickness
            lineRenderer.LineThickness = lineDefaultThickness / zoomScale;
        }
        
        // ================================== UTILS ==================================
        private Vector2 GetNormalizedPosition(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), screenPos,
                null, out Vector2 localPos);
            return localPos / ImageSize;
        }

        private Vector2 GetLocalPointInMap(Vector2 normalizedPos)
        {
            return normalizedPos * ImageSize;
        }

        // ================================== BUTTONS on INSPECTOR ==================================
        [Button("Update Map")]
        private void UpdateMapButton()
        {
            Initialize();
            RenderTerrain();
            OnZoom();
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
            OnZoom();
        }

        [Button("Render Terrain")]
        private void RenderTerrainButton()
        {
            Initialize();
            RenderTerrain();
        }

    }
}