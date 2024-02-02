using ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Map.Rendering
{
    public class MouseCursorInMap : MonoBehaviour, IPointerMoveHandler
    {
        private TMP_Text _label;
        private RectTransform _parentRectTransform;
        private RectTransform _rectTransform;
        private Image _sprite;

        private static Vector2 MousePosition => new(Mouse.current.position.x.value, Mouse.current.position.y.value);
        private Vector2 NormalizedPositionInMap => _parentRectTransform.ScreenToNormalizedPoint(MousePosition);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentRectTransform = _rectTransform.parent.GetComponent<RectTransform>();
            _sprite = _rectTransform.GetComponentInChildren<Image>();
            _label = _rectTransform.GetComponentInChildren<TMP_Text>();
        }

        private void Start()
        {
            MarkerManager.Instance.OnMarkerModeChanged += HandleOnMarkerModeChanged;
        }

        private void Update()
        {
            _rectTransform.position = MousePosition;
        }

        private void OnDestroy()
        {
            MarkerManager.Instance.OnMarkerModeChanged -= HandleOnMarkerModeChanged;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            UpdateCursorDisplay(MarkerManager.Instance.MarkerMode);
        }

        private void HandleOnMarkerModeChanged(MarkerMode markerMode)
        {
            UpdateCursorDisplay(markerMode);
        }

        private void UpdateCursorDisplay(MarkerMode mode)
        {
            switch (mode)
            {
                case MarkerMode.Add:
                    var collisionIndex = MarkerManager.Instance.FindIndex(NormalizedPositionInMap);

                    if (collisionIndex != -1) // Marker in Cursor Position
                    {
                        _sprite.color = Color.yellow;
                        _label.text = "Seleccionar";
                    }
                    else
                    {
                        switch (MarkerManager.Instance.SelectedCount)
                        {
                            case 0: // No Selected
                                _sprite.color = Color.white;
                                _label.text = "Añadir";
                                break;
                            case 1:
                                _sprite.color = Color.yellow;
                                _label.text = "Mover";
                                break;
                            case 2:
                                _sprite.color = Color.yellow;
                                _label.text = "Añadir intermedio";
                                break;
                        }
                    }

                    break;
                case MarkerMode.Remove:
                    _sprite.color = Color.red;
                    _label.text = "Eliminar";
                    break;
                case MarkerMode.Select:
                    _sprite.color = Color.yellow;
                    _label.text = "Seleccionar";
                    break;
                case MarkerMode.None:
                    _sprite.color = Color.white;
                    _label.text = "";
                    break;
            }
        }
    }
}