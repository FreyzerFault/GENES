using ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Map.Rendering
{
    public class MarkerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Marker marker;
        [SerializeField] private float selectedScale = 1.5f;

        private Image _image;

        private MapRendererUI _mapRendererUI;

        private RectTransform _rectTransform;
        private TMP_Text _text;


        public Marker Marker
        {
            get => marker;
            set => marker = value;
        }

        private void Start()
        {
            Initialize();

            marker.OnLabelChange += HandleOnLabelChange;
            marker.OnPositionChange += HandleOnPositionChange;
            marker.OnSelected += HandleOnSelected;
            marker.OnStateChange += HandleOnStateChange;
            MarkerManager.Instance.OnMarkerModeChanged += HandleOnEditMarkerModeChange;
        }

        private void OnDestroy()
        {
            marker.OnLabelChange -= HandleOnLabelChange;
            marker.OnPositionChange -= HandleOnPositionChange;
            marker.OnSelected -= HandleOnSelected;
            marker.OnStateChange -= HandleOnStateChange;
            MarkerManager.Instance.OnMarkerModeChanged -= HandleOnEditMarkerModeChange;
        }

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

        private void Initialize()
        {
            _mapRendererUI = GetComponentInParent<MapRendererUI>();
            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TMP_Text>();
            _image = GetComponentInChildren<Image>();

            _text.text = marker.LabelText;
            _image.color = MarkerManager.Instance.defaultColor;

            UpdatePos(marker.NormalizedPosition);
            UpdateAspect();
        }

        private void HandleOnPositionChange(Vector2 pos) => UpdatePos(pos);
        private void HandleOnSelected(bool selected) => UpdateAspect();
        private void HandleOnStateChange(MarkerState state) => UpdateAspect();
        private void HandleOnEditMarkerModeChange(EditMarkerMode mode) => UpdateAspect();

        private void UpdatePos(Vector2 normalizePos)
        {
            // Rect Anchors to 0,0
            _rectTransform.anchorMax = new Vector2(0, 0);
            _rectTransform.anchorMin = new Vector2(0, 0);

            // Move to Local Pos in Map
            var localPoint = _mapRendererUI.GetComponent<RectTransform>().NormalizedToLocalPoint(normalizePos);
            _rectTransform.anchoredPosition = localPoint;

            // Inverse Zoom to mantain size
            _rectTransform.localScale /= _mapRendererUI.ZoomScale;
        }

        private void HandleOnLabelChange(string label)
        {
            _text.text = label;
        }

        private void UpdateAspect()
        {
            var mm = MarkerManager.Instance;

            // SCALE
            Scale(marker.Selected || marker.hovered ? selectedScale : 1);

            // COLOR
            if (marker.Selected) _image.color = mm.selectedColor;
            else if (marker.hovered)
                _image.color =
                    mm.EditMarkerMode == EditMarkerMode.Delete
                        ? mm.deleteColor
                        : mm.hoverColor;
            else if (marker.IsChecked) _image.color = mm.checkedColor;
            else _image.color = mm.defaultColor;
        }

        private void Scale(float scale) => _rectTransform.localScale = Vector3.one * scale;
    }
}