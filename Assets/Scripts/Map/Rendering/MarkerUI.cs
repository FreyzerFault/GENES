using ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map.Rendering
{
    public class MarkerUI : MonoBehaviour
    {
        public Color defaultColor = Color.white;
        public Color selectedColor = Color.yellow;
        public Color checkedColor = Color.green;

        [SerializeField] private Marker marker;

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
        }

        private void OnDestroy()
        {
            marker.OnLabelChange -= HandleOnLabelChange;
            marker.OnPositionChange -= HandleOnPositionChange;
            marker.OnSelected -= HandleOnSelected;
            marker.OnStateChange -= HandleOnStateChange;
        }

        private void Initialize()
        {
            _mapRendererUI = GetComponentInParent<MapRendererUI>();
            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TMP_Text>();
            _image = GetComponentInChildren<Image>();

            _text.text = marker.LabelText;
            _image.color = defaultColor;

            UpdatePos(marker.NormalizedPosition);
        }

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

        private void HandleOnLabelChange(object sender, string label)
        {
            _text.text = label;
        }

        private void HandleOnPositionChange(object sender, Vector2 pos)
        {
            UpdatePos(pos);
        }

        private void HandleOnSelected(object sender, bool selected)
        {
            _image.color = selected ? selectedColor : defaultColor;
            var selectedScale = 1.5f;
            _image.transform.localScale = Vector3.one * (selected ? selectedScale : 1);
        }

        private void HandleOnStateChange(object sender, MarkerState state)
        {
            _image.color = state == MarkerState.Checked ? checkedColor : _image.color;
        }
    }
}