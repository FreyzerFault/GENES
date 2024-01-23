using System;
using System.Linq;
using ExtensionMethods;
using Map.Markers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Gradient = UnityEngine.Gradient;

#if UNITY_EDITOR
using MyBox;
#endif

#if UNITY_EDITOR
#endif

namespace Map
{
    public class MapUIRenderer : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler, IPointerExitHandler,
        IPointerEnterHandler
    {
        [SerializeField] private float zoomScale = 2;

        public Gradient heightGradient = new();

        [SerializeField] private bool interactable = true;
        [SerializeField] private MarkerMode markerMode = MarkerMode.None;

        [SerializeField] private Transform markersUIParent;

        // LINE
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private int lineDefaultThickness = 7;

        // Icono del Player
        [SerializeField] private RectTransform playerSprite;

        // CURSOR
        [SerializeField] private RectTransform mouseCursorMarker;

        private RawImage _image;
        private RectTransform _rectTransform;

        private TMP_Text MouseLabel => mouseCursorMarker.GetComponentInChildren<TMP_Text>();
        private Image MouseSprite => mouseCursorMarker.GetComponentInChildren<Image>();

        // MARKERS
        public MarkerManagerSO MarkerManager => MapManager.Instance.markerManager;

        public float ZoomScale
        {
            get => zoomScale;
            set
            {
                zoomScale = value;
                Zoom();
            }
        }

        public RawImage Image
        {
            get => _image ? _image : GetComponent<RawImage>();
            private set => _image = value;
        }

        private float ImageWidth => Image.rectTransform.rect.width;
        private float ImageHeight => Image.rectTransform.rect.height;
        private Vector2 ImageSize => new(ImageWidth, ImageHeight);

        private Vector2 OriginPoint
        {
            get
            {
                var corners = new Vector3[4];
                Image.rectTransform.GetWorldCorners(corners);
                return corners[0];
            }
        }

        // ================================== UNITY ==================================
        private void Awake()
        {
            Image ??= GetComponent<RawImage>();
        }

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            Initialize();
            RenderTerrain();
            Zoom();
        }

        private void Update()
        {
            if (interactable)
            {
                markerMode = MarkerMode.Add;

                // SHIFT => Remove Mode
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift)) markerMode = MarkerMode.Remove;

                UpdateMouseMarker();
            }

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
                case MarkerMode.None:
                default:
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable || mouseCursorMarker == null) return;
            mouseCursorMarker.gameObject.SetActive(true);
            Cursor.visible = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable || mouseCursorMarker == null) return;
            mouseCursorMarker.gameObject.SetActive(false);
            Cursor.visible = true;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!interactable || mouseCursorMarker == null) return;
            mouseCursorMarker.position = eventData.position;
        }

        // ================================== INITIALIZATION ==================================

        private void Initialize()
        {
            // SUBSCRIBERS:
            MarkerManager.OnMarkerAdded.AddListener(HandleAdded);
            MarkerManager.OnMarkerRemoved.AddListener(HandleRemoved);
            MarkerManager.OnMarkerMoved.AddListener(HandleMoved);
            MarkerManager.OnMarkersClear.AddListener(HandleClear);


            // TODO QUITAR ESTO Y LLEVARLO A UN MAPCURSOR
            MarkerManager.OnMarkerSelected.AddListener(HandleMarkerSelected);
            MarkerManager.OnMarkerDeselected.AddListener(HandleMarkerDeselected);


            // MARKERS
            UpdateMarkers();
        }


        // ================================== EVENT SUSCRIBERS ==================================
        private void HandleAdded(Marker marker, int index)
        {
            InstantiateMarker(marker);
            AddMarkerToLine(marker, index);
            UpdateMouseMarker();
        }

        private void HandleRemoved(Marker marker, int index)
        {
            DestroyMarkerUI(marker);
            RemoveMarkerFromLine(index);
            UpdateMouseMarker();
        }

        private void HandleMoved(Marker marker, int index)
        {
            MoveMarkerFromLine(marker, index);
            UpdateMouseMarker();
        }

        private void HandleClear()
        {
            ClearMarkersUI();
            ClearLineRenderer();
        }

        private void HandleMarkerSelected(Marker marker)
        {
            UpdateMouseMarker();
        }

        private void HandleMarkerDeselected(Marker marker)
        {
            UpdateMouseMarker();
        }

        // ================================== TERRAIN VISUALIZATION ==================================
        private void RenderTerrain()
        {
            // Create Texture of Map
            Image.texture =
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
            var displacement = ImageSize / 3;

            // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
            var distanceToBotLeft = MapManager.Instance.PlayerDistanceToBotLeftBorder * mapSizeScaled;
            displacement.x = Mathf.Min(distanceToBotLeft.x, displacement.x);
            displacement.y = Mathf.Min(distanceToBotLeft.y, displacement.y);

            // Pivot reajustado
            Image.rectTransform.pivot = pivot;
            Image.rectTransform.anchoredPosition = displacement;
        }

        private void Zoom()
        {
            // Escalar el mapa relativo al centro donde está el Player
            Image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

            // La flecha del player se escala al revés para que no se vea afectada por el zoom
            playerSprite.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);
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
            ClearLineRenderer();
        }

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (var marker in MarkerManager.Markers) InstantiateMarker(marker);

            UpdateLineRenderer();
            UpdateMouseMarker();
        }

        private void InstantiateMarker(Marker marker)
        {
            var markerUI = Instantiate(MarkerManager.markerUIPrefab, markersUIParent).GetComponent<MarkerUI>();
            markerUI.Marker = marker;
        }

        private void DestroyMarkerUI(Marker marker)
        {
            var markerUI = markersUIParent.GetComponentsInChildren<MarkerUI>().ToList()
                .Find(m => m.Marker.Equals(marker));

            if (Application.isPlaying)
                Destroy(markerUI.gameObject);
            else
                DestroyImmediate(markerUI.gameObject);
        }

        public void ToggleMarkers(bool value)
        {
            markersUIParent.gameObject.SetActive(value);
        }


        // ================================== MOUSE MARKER ==================================

        private void UpdateMouseMarker()
        {
            if (mouseCursorMarker == null || !mouseCursorMarker.gameObject.activeSelf) return;

            switch (markerMode)
            {
                case MarkerMode.Add:

                    var collisionIndex =
                        MarkerManager.FindClosestMarkerIndex(GetNormalizedPosition(
                            new Vector2(Mouse.current.position.x.value, Mouse.current.position.y.value)));
                    if (collisionIndex != -1)
                    {
                        MouseSprite.color = Color.yellow;
                        MouseLabel.text = "Seleccionar";
                    }
                    else
                    {
                        switch (MarkerManager.numSelectedMarkers)
                        {
                            case 0:
                                MouseSprite.color = Color.white;
                                MouseLabel.text = "Añadir";
                                break;
                            case 1:
                                MouseSprite.color = Color.yellow;
                                MouseLabel.text = "Mover";
                                break;
                            case 2:
                                MouseSprite.color = Color.yellow;
                                MouseLabel.text = "Añadir intermedio";
                                break;
                        }
                    }

                    break;

                case MarkerMode.Remove:
                    MouseSprite.color = Color.red;
                    MouseLabel.text = "Eliminar";
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
            var lineWithDefaultPoint = lineRenderer.Points[0] == Vector2.zero;

            if (index == -1)
            {
                lineRenderer.Points = lineRenderer.Points.Append(
                    GetLocalPointInMap(marker.NormalizedPosition)
                ).ToArray();
            }
            else
            {
                var posInMap = GetLocalPointInMap(marker.NormalizedPosition);
                var points = lineRenderer.Points;
                lineRenderer.Points = points
                    .Take(index)
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
            var emptyArray = Array.Empty<Vector2>();
            lineRenderer.Points = emptyArray;
        }

        // ================================== UTILS ==================================
        private Vector2 GetNormalizedPosition(Vector2 screenPos)
        {
            _rectTransform ??= GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPos,
                null, out var localPos);
            return localPos / ImageSize;
        }

        public Vector2 GetLocalPointInMap(Vector2 normalizedPos)
        {
            return normalizedPos * ImageSize;
        }

        [Serializable]
        private enum MarkerMode
        {
            Add,
            Remove,
            Select,
            None
        }

#if UNITY_EDITOR
        // ================================== BUTTONS on INSPECTOR ==================================
        [ButtonMethod]
        private void UpdateMap()
        {
            Initialize();
            RenderTerrain();
            Zoom();
            UpdatePlayerPoint();
        }

        // BUTTONS
        [ButtonMethod]
        private void UpdatePlayerPointInMap()
        {
            Initialize();
            UpdatePlayerPoint();
        }

        [ButtonMethod]
        private void ZoomMapToPlayerPosition()
        {
            Initialize();
            Zoom();
        }


        [ButtonMethod]
        private void ReRenderTerrainButton()
        {
            Initialize();
            RenderTerrain();
        }
#endif
    }
}