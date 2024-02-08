using ExtensionMethods;
using TMPro;
using UnityEngine;

namespace Map.Rendering
{
    internal enum CursorDisplayMode
    {
        Default,
        Delete,
        Select,
        DefaultIllegal,
        SelectIllegal
    }

    public class MouseCursorInMap : MonoBehaviour
    {
        [SerializeField] private Texture2D cursorTexture;
        [SerializeField] private Texture2D cursorDeleteTexture;
        [SerializeField] private Texture2D cursorSelectTexture;
        [SerializeField] private Texture2D cursorIllegalTexture;
        [SerializeField] private Texture2D cursorIllegalSelectedTexture;

        private CursorDisplayMode _displayMode;

        private TMP_Text _label;
        private RectTransform _parentRectTransform;
        private RectTransform _rectTransform;

        private CursorDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (value != _displayMode)
                    SetCursorDisplayTexture(value);
                _displayMode = value;
            }
        }


        private static Vector2 MousePosition => Input.mousePosition;
        private Vector2 NormalizedPositionInMap => _parentRectTransform.ScreenToNormalizedPoint(MousePosition);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentRectTransform = _rectTransform.parent.GetComponent<RectTransform>();
            _label = _rectTransform.GetComponentInChildren<TMP_Text>();

            UpdateCursorTexture(cursorTexture);
        }

        private void Update()
        {
            _rectTransform.position = MousePosition;
            UpdateCursorMode();
        }

        private bool OutOfMap()
        {
            var normPos = NormalizedPositionInMap;
            return normPos.x < 0 || normPos.y < 0 || normPos.x > 1 || normPos.y > 1;
        }

        private void UpdateCursorMode()
        {
            if (OutOfMap())
            {
                DisplayMode = CursorDisplayMode.Default;
                _label.text = "";
                return;
            }

            switch (MarkerManager.Instance.EditMarkerMode)
            {
                case EditMarkerMode.Add:
                    if (MarkerManager.Instance.AnyHovered)
                    {
                        DisplayMode = CursorDisplayMode.Select;
                        _label.text = "Seleccionar";
                    }
                    else
                    {
                        // Añadir o Mover marker => Necesita una posición legal
                        var isLegal = MapManager.Instance.IsLegalPos(NormalizedPositionInMap);
                        switch (MarkerManager.Instance.SelectedCount)
                        {
                            case 0: // No Selected
                                DisplayMode = isLegal ? CursorDisplayMode.Default : CursorDisplayMode.DefaultIllegal;
                                _label.text = "Añadir";
                                break;
                            case 1:
                                DisplayMode = isLegal ? CursorDisplayMode.Select : CursorDisplayMode.SelectIllegal;
                                _label.text = "Mover";
                                break;
                            case 2:
                                DisplayMode = isLegal ? CursorDisplayMode.Select : CursorDisplayMode.SelectIllegal;
                                _label.text = "Añadir intermedio";
                                break;
                        }
                    }

                    break;
                case EditMarkerMode.Delete:
                    DisplayMode = CursorDisplayMode.Delete;
                    _label.text = "Eliminar";
                    break;
                case EditMarkerMode.Select:
                    DisplayMode = CursorDisplayMode.Select;
                    _label.text = "Seleccionar";
                    break;
                case EditMarkerMode.None:
                    DisplayMode = CursorDisplayMode.Default;
                    _label.text = "";
                    break;
            }
        }

        private void SetCursorDisplayTexture(CursorDisplayMode newDisplayMode)
        {
            switch (newDisplayMode)
            {
                case CursorDisplayMode.Default:
                    UpdateCursorTexture(cursorTexture);
                    break;
                case CursorDisplayMode.Select:
                    UpdateCursorTexture(cursorSelectTexture);
                    break;
                case CursorDisplayMode.Delete:
                    UpdateCursorTexture(cursorDeleteTexture);
                    break;
                case CursorDisplayMode.DefaultIllegal:
                    UpdateCursorTexture(cursorIllegalTexture);
                    break;
                case CursorDisplayMode.SelectIllegal:
                    UpdateCursorTexture(cursorIllegalSelectedTexture);
                    break;
            }
        }

        private void UpdateCursorTexture(Texture2D tex) =>
            Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto);
    }
}