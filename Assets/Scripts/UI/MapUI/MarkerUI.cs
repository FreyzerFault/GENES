using DavidUtils.ExtensionMethods;
using Markers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.MapUI
{
    public class MarkerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] public Marker marker;

        [FormerlySerializedAs("selectedScale")] [SerializeField] private float hoveredScale = 1.5f;

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
            marker.OnHovered += OnHovered;
            MarkerManager.Instance.OnMarkerModeChanged += HandleOnEditMarkerModeChange;

            UpdatePos(marker.NormalizedPosition);
            UpdateAspect();
        }

        private void OnDestroy()
        {
            marker.OnLabelChange -= HandleOnLabelChange;
            marker.OnPositionChange -= HandleOnPositionChange;
            marker.OnSelected -= HandleOnSelected;
            marker.OnStateChange -= HandleOnStateChange;
            marker.OnHovered -= OnHovered;
            MarkerManager.Instance.OnMarkerModeChanged -= HandleOnEditMarkerModeChange;
        }

        

        // Map Events
        private void HandleOnEditMarkerModeChange(EditMarkerMode mode) => UpdateAspect();

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
            _rectTransform.anchoredPosition = normalizePos * _parentRectTransform.rect.size;
        }

        #region VISUALS

        public Color checkedColor = Color.green;
        public Color defaultColor = Color.white;
        public Color deleteColor = Color.red;
        public Color hoverColor = Color.blue;
        public Color selectedColor = Color.yellow;

        private void UpdateColor()
        {
            if (marker.Hovered)
                _image.color = MarkerManager.Instance.EditMarkerMode == EditMarkerMode.Delete
                    ? deleteColor
                    : marker.Selected ? selectedColor : hoverColor;
            else if (marker.Selected) _image.color = selectedColor;
            else if (marker.IsChecked) _image.color = checkedColor;
            else _image.color = defaultColor;
        }

        private void UpdateAspect()
        {
            // LABEL
            _text.text = marker.LabelText;

            // COLOR
            UpdateColor();
        }

        public void UpdateScaleByZoom(float zoom) => 
            _rectTransform.localScale = (Vector3.one * (marker.Hovered ? hoveredScale : 1)) / zoom;

        #region HOVER

        public void OnPointerEnter(PointerEventData eventData) => marker.Hovered = true;
        public void OnPointerExit(PointerEventData eventData) => marker.Hovered = false;
        
        private void OnHovered(bool hover)
        {
            UpdateColor();
            
            if (marker.Selected) return;
            
            if (hover) _rectTransform.localScale *= hoveredScale;
            else _rectTransform.localScale /= hoveredScale;
        }

        #endregion

        #endregion
    }
}
