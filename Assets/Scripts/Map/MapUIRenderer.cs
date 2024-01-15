using System;
using System.Linq;
using EditorCools;
using ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [SerializeField] private Transform mapMarkersParent;

        // LINE
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private int lineDefaultThickness = 7;

        // Objetos que siguen al cursor
        [SerializeField] private RectTransform mouseCursorMarker;
        [SerializeField] private RectTransform mouseLabel;
        [SerializeField] private RectTransform _playerSprite;

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

        public bool RemoveMarkersMode
        {
            get => markerMode == MarkerMode.Remove;
            set => markerMode = value ? MarkerMode.Remove : MarkerMode.Add;
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
        private void Start()
        {
            Initialize();
            RenderTerrain();
            Zoom();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift)) markerMode = MarkerMode.Remove;

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
                    MarkerManager.AddOrSelectPoint(normalizedPosition, out var _, out var _);
                    break;
                case MarkerMode.Remove:
                    MarkerManager.RemovePoint(normalizedPosition);
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
            _image ??= GetComponent<RawImage>();
            _playerSprite ??= GetComponentInChildren<Image>().rectTransform;


            // MARKERS
            UpdateMarkers();
            MarkerManager.OnMarkerAdded.AddListener(_ => UpdateMarkers());
            MarkerManager.OnMarkerRemoved.AddListener(_ => UpdateMarkers());
            MarkerManager.OnMarkersClear.AddListener(UpdateMarkers);
            MarkerManager.OnMarkerMoved.AddListener(_ => UpdateMarkers());
            MarkerManager.OnMarkerSelected.AddListener(_ => UpdateMarkers());
            MarkerManager.OnMarkerDeselected.AddListener(_ => UpdateMarkers());

            // Mouse Cursor
            MarkerManager.OnMarkerSelected.AddListener(_ => UpdateMouseMarker());
            MarkerManager.OnMarkerDeselected.AddListener(_ => UpdateMouseMarker());
        }

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
            _playerSprite.anchoredPosition = MapManager.Instance.PlayerNormalizedPosition * ImageSize;
            _playerSprite.rotation = MapManager.Instance.PlayerRotationForUI;

            if (zoomScale != 1) CenterPlayerInZoomedMap();
        }

        private void CenterPlayerInZoomedMap()
        {
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

        public void Zoom()
        {
            // Escalar el mapa relativo al centro donde está el Player
            _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            _playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);

            // UpdateMarkers();
        }


        // ================================== MARKERS ==================================

        private void ClearMarkers()
        {
            GetComponentsInChildren<MarkerUI>().ToList().ForEach(marker => Destroy(marker.gameObject));
        }

        private void UpdateMarkers()
        {
            // Clear Markers UI
            ClearMarkers();

            // Se instancian de nuevo
            // Y se actualizan sus etiquetas en orden
            for (var i = 0; i < MarkerManager.MarkersCount; i++)
            {
                var marker = MarkerManager.Markers[i];
                marker.labelText = marker.selected
                    ? $"{i} - SELECTED"
                    : $"{i} - {Vector3Int.RoundToInt(marker.worldPosition)}";
                marker.color = marker.selected ? MarkerManager.selectedColor : MarkerManager.markerColor;
                InstantiateMarker(marker);
            }

            // Line Renderer y Mouse Cursor
            UpdateLine();
            UpdateMouseMarker();
        }

        private void InstantiateMarker(Marker marker)
        {
            var marker = Instantiate(MarkerManager.markerUIPrefab, mapMarkersParent).GetComponent<MarkerUI>();
            marker.SetData(markerData);
            var markerRect = marker.GetComponent<RectTransform>();
            // marker.GetComponent<RectTransform>().localPosition = GetLocalPointInMap(markerData.normalizedPosition);
            markerRect.anchorMax = new Vector2(0, 0);
            markerRect.anchorMin = new Vector2(0, 0);
            // markerRect.pivot = markerData.normalizedPosition;
            markerRect.anchoredPosition = GetLocalPointInMap(markerData.normalizedPosition);

            markerRect.localScale /= zoomScale;
        }

        public void ToggleMarkers(bool value)
        {
            mapMarkersParent.gameObject.SetActive(value);
        }


        // ================================== MOUSE MARKER ==================================

        private void UpdateMouseMarker()
        {
            if (mouseCursorMarker == null || !mouseCursorMarker.gameObject.activeSelf) return;

            var numSelected = MarkerManager.numSelectedMarkers;
            if (numSelected == 0) return;
            if (numSelected == 1)
            {
                mouseCursorMarker.GetComponent<Image>().color = Color.red;
                mouseLabel.GetComponent<TMP_Text>().text = "Place Marker Selected";
            }
            else if (numSelected == 2)
            {
                mouseCursorMarker.GetComponent<Image>().color = Color.red;
                mouseLabel.GetComponent<TMP_Text>().text = "Click to add Middle Marker";
            }
        }

        // ================================== LINE RENDERER ==================================
        private void UpdateLine()
        {
            lineRenderer.Points = MarkerManager.Markers.ToList().ConvertAll(
                marker => GetLocalPointInMap(marker.normalizedPosition)
            ).ToArray();

            // Thickness
            lineRenderer.LineThickness = lineDefaultThickness / zoomScale;
        }

        // ================================== UTILS ==================================
        private Vector2 GetNormalizedPosition(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), screenPos,
                null, out var localPos);
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