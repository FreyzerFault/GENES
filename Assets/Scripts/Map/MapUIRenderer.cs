using System;
using System.Linq;
using ExtensionMethods;
using Map.Markers;
using Map.Path;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Gradient = UnityEngine.Gradient;

#if UNITY_EDITOR
using MyBox;
#endif

#if UNITY_EDITOR
#endif

namespace Map
{
    [Serializable]
    public enum MarkerMode
    {
        Add,
        Remove,
        Select,
        None
    }

    public class MapUIRenderer : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler, IPointerExitHandler,
        IPointerEnterHandler
    {
        [SerializeField] private float zoomScale = 2;

        public Gradient heightGradient = new();

        [SerializeField] private bool interactable = true;
        [SerializeField] private MarkerMode markerMode = MarkerMode.None;

        [SerializeField] private Transform markersUIParent;

        // LINE Renderers
        [SerializeField] private PathRendererUI pathRenderer;

        [SerializeField] private int lineDefaultThickness = 7;

        // Icono del Player
        [SerializeField] private RectTransform playerSprite;

        // CURSOR
        [SerializeField] private RectTransform mouseCursorMarker;

        private RectTransform _rectTransform;

        private TMP_Text MouseLabel => mouseCursorMarker.GetComponentInChildren<TMP_Text>();
        private Image MouseSprite => mouseCursorMarker.GetComponentInChildren<Image>();

        // MARKERS
        public MarkerManager MarkerManager => MarkerManager.Instance;

        public float ZoomScale
        {
            get => zoomScale;
            set
            {
                zoomScale = value;
                Zoom();
            }
        }

        public RawImage Image { get; private set; }

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
            Image = GetComponent<RawImage>();
            pathRenderer = GetComponentInChildren<PathRendererUI>();
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
            var normalizedPosition = _rectTransform.ScreenToNormalizedPoint(eventData.position);

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
            MarkerManager.OnMarkerAdded += HandleAdded;
            MarkerManager.OnMarkerRemoved += HandleRemoved;
            MarkerManager.OnMarkerMoved += HandleMoved;
            MarkerManager.OnMarkersClear += HandleClear;


            // TODO QUITAR ESTO Y LLEVARLO A UN MAPCURSOR
            MarkerManager.OnMarkerSelected += HandleMarkerSelected;
            MarkerManager.OnMarkerDeselected += HandleMarkerDeselected;


            // PATH RENDERER
            // TODO
            PathGenerator.Instance.OnPathAdded += AddPath;
            PathGenerator.Instance.OnPathUpdated += UpdatePath;
            PathGenerator.Instance.OnPathDeleted += RemovePath;
            PathGenerator.Instance.OnPathsCleared += ClearPathRenderers;

            // MARKERS
            UpdateMarkers();
        }


        // ================================== EVENT SUSCRIBERS ==================================
        private void HandleAdded(Marker marker, int index)
        {
            InstantiateMarker(marker);
            UpdateMouseMarker();
        }

        private void HandleRemoved(Marker marker, int index)
        {
            DestroyMarkerUI(marker);
            UpdateMouseMarker();
        }

        private void HandleMoved(Marker marker, int index)
        {
            UpdateMouseMarker();
        }

        private void HandleClear()
        {
            ClearMarkersUI();
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
                MapManager.Instance.terrain.ToTexture((int)ImageWidth, (int)ImageHeight, heightGradient);
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
            ClearPathRenderers();
        }

        private void UpdateMarkers()
        {
            ClearMarkersUI();

            // Se instancian de nuevo todos los markers
            foreach (var marker in MarkerManager.Markers) InstantiateMarker(marker);

            UpdateAllPathRenderers(PathGenerator.Instance.paths.ToArray());
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
                        MarkerManager.FindIndex(_rectTransform.ScreenToNormalizedPoint(
                            new Vector2(Mouse.current.position.x.value, Mouse.current.position.y.value)));
                    if (collisionIndex != -1)
                    {
                        MouseSprite.color = Color.yellow;
                        MouseLabel.text = "Seleccionar";
                    }
                    else
                    {
                        switch (MarkerManager.SelectedCount)
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

        // ================================== PATH RENDERERs ==================================
        private void UpdateAllPathRenderers(PathFinding.Path[] paths)
        {
            pathRenderer.UpdateAllLines(paths);
        }

        private void UpdatePathRenderer(PathFinding.Path path, int index)
        {
            pathRenderer.UpdateLine(path, index);
        }

        private void AddPath(PathFinding.Path path, int index = -1)
        {
            pathRenderer.AddPath(path, index);
        }

        // Modificar un Path por indice
        private void UpdatePath(PathFinding.Path path, int index)
        {
            pathRenderer.UpdateLine(path, index);
        }

        private void RemovePath(int index = -1)
        {
            pathRenderer.RemovePath(index);
        }

        private void ClearPathRenderers()
        {
            pathRenderer.ClearPaths();
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