using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Client.Common;
using EditorCools;
using ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Gradient = UnityEngine.Gradient;

namespace Map
{
    public class MapUIRenderer : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler, IPointerExitHandler,
        IPointerEnterHandler
    {
        [SerializeField] private float zoomScale = 2;

        public Gradient heightGradient = new();
        [SerializeField] private MarkerMode markerMode = MarkerMode.None;

        [SerializeField] private Transform markersUIParent;

        // LINE
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private int lineDefaultThickness = 7;

        // Objetos que siguen al cursor
        [SerializeField] private RectTransform mouseCursorMarker;
        [SerializeField] private RectTransform mouseLabel;
        [SerializeField] private RectTransform playerSprite;

        private RawImage _image;

        // MARKERS
        private MapMarkerManagerSO MarkerManager => MapManager.Instance.markerManager;

        public float ZoomScale
        {
            get => zoomScale;
            set
            {
                zoomScale = value;
                Zoom();
            }
        }


        private float ImageWidth => _image.rectTransform.rect.width;
        private float ImageHeight => _image.rectTransform.rect.height;
        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        private Vector2 OriginPoint
        {
            get
            {
                var corners = new Vector3[4];
                _image.rectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        // ================================== UNITY ==================================
        private void Awake()
        {
            _image ??= GetComponent<RawImage>();
        }

        private void Start()
        {
            Initialize();
            RenderTerrain();
            Zoom();
        }

        private void Update()
        {
            // SHIFT => Remove Mode
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift)) markerMode = MarkerMode.Remove;

            UpdatePlayerPoint();
        }

        // ================================== MOUSE EVENTS ==================================
        public void OnPointerClick(PointerEventData eventData)
        {
            if (markerMode == MarkerMode.None) return;

            // [0,1] Position
            var normalizedPosition = GetNormalizedPosition(eventData.position);

            switch (markerMode)
            {
                case MarkerMode.Add:
                    MarkerManager.AddOrSelectMarker(normalizedPosition);
                    break;
                case MarkerMode.Remove:
                    MarkerManager.RemoveMarker(normalizedPosition);
                    break;
                case MarkerMode.Select:
                    MarkerManager.SelectMarker(normalizedPosition);
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
            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded.AddListener(HandleMarkerAdded);
            MarkerManager.OnMarkerRemoved.AddListener(HandleMarkerRemoved);
            MarkerManager.OnMarkerMoved.AddListener(HandleMarkerMoved);
            MarkerManager.OnMarkersClear.AddListener(HandleMarkersClear);
            
            
            // TODO QUITAR ESTO Y LLEVARLO A UN MAPCURSOR
            MarkerManager.OnMarkerSelected.AddListener(HandleMarkerSelected);
            MarkerManager.OnMarkerDeselected.AddListener(HandleMarkerDeselected);
            
            
            // MARKERS
            UpdateMarkers();
        }


        // ================================== EVENT SUSCRIBERS ==================================
        private void HandleMarkerAdded(Marker marker, int index)
        {
            InstantiateMarker(marker);
            AddMarkerToLine(marker, index);
        }
        private void HandleMarkerRemoved(Marker marker, int index)
        {
            UpdateMarkers();
            RemoveMarkerFromLine(index);
        }
        private void HandleMarkerMoved(Marker marker, int index)
        {
            UpdateMarkers();
            RemoveMarkerFromLine(index);
            AddMarkerToLine(marker, index);
        }
        private void HandleMarkersClear()
        {
            ClearMarkersUI();
            ClearLineRenderer();
        }
        private void HandleMarkerSelected(Marker marker) => UpdateMouseMarker();
        private void HandleMarkerDeselected(Marker marker) => UpdateMouseMarker();

        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            // Create Texture of Map
            _image.texture =
                MapManager.Instance.TerrainData.ToTexture((int)ImageWidth, (int)ImageHeight, heightGradient);
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
            if (zoomScale == 1) return;
            
            // Centrar el Player en el centro del minimapa por medio de su pivot
            var pivot = MapManager.Instance.PlayerNormalizedPosition;

            // Tamaño del minimapa escalado y Normalizado
            var mapSizeScaled = ImageSize * zoomScale;
            var displacement = ImageSize / 2;

            // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
            var distanceToBotLeft = MapManager.Instance.PlayerDistanceToBotLeftBorder * mapSizeScaled;
            displacement.x = Mathf.Min(distanceToBotLeft.x, displacement.x);
            displacement.y = Mathf.Min(distanceToBotLeft.y, displacement.y);

            // Pivot reajustado
            _image.rectTransform.pivot = pivot;
            _image.rectTransform.anchoredPosition = displacement;
        }

        private void Zoom()
        {
            // Escalar el mapa relativo al centro donde está el Player
            _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);
        }


        // ================================== MARKERS ==================================

        private void ClearMarkersUI()
        {
            GetComponentsInChildren<MarkerUI>().ToList().ForEach(marker => Destroy(marker.gameObject));
            ClearLineRenderer();
        }

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (Marker marker in MarkerManager.Markers) InstantiateMarker(marker);

            UpdateLineRenderer();
            UpdateMouseMarker();
        }

        private void InstantiateMarker(Marker marker)
        {
            var markerUI = Instantiate(MarkerManager.markerUIPrefab, markersUIParent).GetComponent<MarkerUI>();
            markerUI.Data = marker;
        }

        public void ToggleMarkers(bool value)
        {
            markersUIParent.gameObject.SetActive(value);
        }


        // ================================== MOUSE MARKER ==================================

        private void UpdateMouseMarker()
        {
            if (mouseCursorMarker == null || !mouseCursorMarker.gameObject.activeSelf) return;

            switch (MarkerManager.numSelectedMarkers)
            {
                case 0:
                    return;
                case 1:
                    mouseCursorMarker.GetComponent<Image>().color = Color.red;
                    mouseLabel.GetComponent<TMP_Text>().text = "Move Marker Selected";
                    break;
                case 2:
                    mouseCursorMarker.GetComponent<Image>().color = Color.red;
                    mouseLabel.GetComponent<TMP_Text>().text = "Add Middle Marker";
                    break;
            }
        }

        // ================================== LINE RENDERER ==================================
        private void UpdateLineRenderer()
        {
            lineRenderer.Points = MarkerManager.Markers.ToList().ConvertAll(
                marker => GetLocalPointInMap(marker.NormalizedPosition)
            ).ToArray();

            // Thickness
            lineRenderer.LineThickness = lineDefaultThickness / zoomScale;
        }

        private void AddMarkerToLine(Marker marker, int index = -1)
        {
            // Si el primer punto es el default, se quita
            bool lineWithDefaultPoint = lineRenderer.Points[0] == Vector2.zero;
            
            if (index == -1) 
                lineRenderer.Points = lineRenderer.Points.Append(
                        GetLocalPointInMap(marker.NormalizedPosition)
                    ).ToArray();
            else
            {
                Vector2 posInMap = GetLocalPointInMap(marker.NormalizedPosition);
                Vector2[] points = lineRenderer.Points;
                lineRenderer.Points = points
                    .Take(index - 1)
                    .Append(posInMap)
                    .Concat(points.TakeLast(points.Length - index))
                    .ToArray();
            }
            
            if (lineWithDefaultPoint) RemoveMarkerFromLine(0);
        }
        
        private void RemoveMarkerFromLine(int index = -1)
        {
            if (index == -1) return;

            var list = lineRenderer.Points.ToList();
            list.RemoveAt(index);
            lineRenderer.Points = list.ToArray();
        }
        
        private void MoveMarkerFromLine(Marker marker, int index = -1)
        {
            RemoveMarkerFromLine(index);
            AddMarkerToLine(marker, index);
        }
        
        private void ClearLineRenderer()
        {
            Vector2[] emptyArray = Array.Empty<Vector2>();
            lineRenderer.Points = emptyArray;
        }

        // ================================== UTILS ==================================
        private Vector2 GetNormalizedPosition(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), screenPos,
                null, out var localPos);
            return localPos / ImageSize;
        }

        public Vector2 GetLocalPointInMap(Vector2 normalizedPos)
        {
            return normalizedPos * ImageSize;
        }

        // ================================== BUTTONS on INSPECTOR ==================================
        [Button("Update Map")]
        private void UpdateMapButton()
        {
            Initialize();
            RenderTerrain();
            Zoom();
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
            Zoom();
        }

        [Button("Render Terrain")]
        private void RenderTerrainButton()
        {
            Initialize();
            RenderTerrain();
        }

        [Serializable]
        private enum MarkerMode
        {
            Add,
            Remove,
            Select,
            None
        }
    }
}