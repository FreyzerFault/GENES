using DavidUtils.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Map.Rendering
{
    public class MarkerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] public Marker marker;
        [SerializeField] private float selectedScale = 1.5f;

        private Image _image;
        private TMP_Text _text;
        
        private RectTransform _parentRectTransform; 
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TMP_Text>();
            _image = GetComponentInChildren<Image>();
            _parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        private void Start()
        {
            marker.OnLabelChange += HandleOnLabelChange;
            marker.OnPositionChange += HandleOnPositionChange;
            marker.OnSelected += HandleOnSelected;
            marker.OnStateChange += HandleOnStateChange;
            MarkerManager.Instance.OnMarkerModeChanged += HandleOnEditMarkerModeChange;
            MapManager.Instance.OnZoomMapChanged += HandleOnZoomMapChange;
            
            UpdatePos(marker.NormalizedPosition);
            UpdateAspect();
        }

        private void OnDestroy()
        {
            marker.OnLabelChange -= HandleOnLabelChange;
            marker.OnPositionChange -= HandleOnPositionChange;
            marker.OnSelected -= HandleOnSelected;
            marker.OnStateChange -= HandleOnStateChange;
            MarkerManager.Instance.OnMarkerModeChanged -= HandleOnEditMarkerModeChange;
            MapManager.Instance.OnZoomMapChanged -= HandleOnZoomMapChange;
        }

        // Pointer Events
        public void OnPointerEnter(PointerEventData eventData)
        {
            marker.hovered = true;
            UpdateAspect();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            marker.hovered = false;
            UpdateAspect();
        }
        
        // Map Events
        private void HandleOnEditMarkerModeChange(EditMarkerMode mode) => UpdateAspect();
        private void HandleOnZoomMapChange(float modezoom) => UpdateScaleByZoom();
        
        // Marker Events
        private void HandleOnPositionChange(Vector2 pos) => UpdatePos(pos);
        private void HandleOnLabelChange(string label) => _text.text = label;
        private void HandleOnStateChange(MarkerState state) => UpdateAspect();
        private void HandleOnSelected(bool selected) => UpdateAspect();
        
        // Move to it's position 
        private void UpdatePos(Vector2 normalizePos)
        {
            // Rect Anchors to 0,0
            _rectTransform.anchorMax = new Vector2(0, 0);
            _rectTransform.anchorMin = new Vector2(0, 0);

            // Move to Local Pos in Map
            Vector2 localPoint = _parentRectTransform.NormalizedToLocalPoint(normalizePos);
            _rectTransform.anchoredPosition = localPoint;
        }


        #region VISUALS
        
        public Color checkedColor = Color.green;
        public Color defaultColor = Color.white;
        public Color deleteColor = Color.red;
        public Color hoverColor = Color.blue;
        public Color selectedColor = Color.yellow;

        
        private void UpdateAspect()
        {
            // LABEL
            _text.text = marker.LabelText;

            // SCALE
            UpdateScaleByZoom();
            float scale = (marker.Selected || marker.hovered) ? selectedScale : 1;
            _rectTransform.localScale *= scale;

            // COLOR
            if (marker.Selected)
                _image.color = selectedColor;
            else if (marker.hovered)
                _image.color =
                    MarkerManager.Instance.EditMarkerMode == EditMarkerMode.Delete ? deleteColor : hoverColor;
            else if (marker.IsChecked)
                _image.color = checkedColor;
            else
                _image.color = defaultColor;
        }
        
        // Inverse Zoom to mantain size
        private void UpdateScaleByZoom() =>
            _rectTransform.localScale = Vector3.one / MapManager.Instance.Zoom;
        
        #endregion

    }
}