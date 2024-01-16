using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class MarkerUI : MonoBehaviour
    {
        public Color defaultColor = Color.white;
        public Color selectedColor = Color.yellow;
        public Color checkedColor = Color.green;

        [SerializeField] private Marker marker;

        private Image _image;

        private MapUIRenderer _mapUIRenderer;

        private RectTransform _rectTransform;
        private TMP_Text _text;

        public Marker Data
        {
            set => marker = value;
        }

        public Guid Id => marker.id;

        private void Start()
        {
            Initialize();

            marker.onLabelChange.AddListener(label => _text.text = label);
            marker.onPositionChange.AddListener(UpdatePos);
            marker.onSelected.AddListener(selected => _image.color = selected ? selectedColor : _image.color);
            marker.onStateChange.AddListener(state =>
                _image.color = state == MarkerState.Checked ? checkedColor : _image.color);
        }

        private void Initialize()
        {
            _mapUIRenderer = GetComponentInParent<MapUIRenderer>();
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
            _rectTransform.anchoredPosition = _mapUIRenderer.GetLocalPointInMap(normalizePos);

            // Inverse Zoom to mantain size
            _rectTransform.localScale /= _mapUIRenderer.ZoomScale;
        }
    }
}