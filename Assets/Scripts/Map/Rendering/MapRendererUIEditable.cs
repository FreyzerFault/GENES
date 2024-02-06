using ExtensionMethods;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Map.Rendering
{
    public class MapRendererUIEditable : MapRendererUI, IDragHandler, IPointerUpHandler,
        IPointerDownHandler
    {
        [SerializeField] private GameObject markerPlaceholderDraggedPrefab;

        private readonly float _minDragDistanceToMove = 0.05f;
        private bool _isDragging;
        private int _markerDraggedIndex = -1;
        private GameObject _markerPlaceholderDragged;

        private bool IsDraggingMarker => _isDragging && _markerDraggedIndex != -1;

        // ============================= MOUSE EVENTS =============================
        public void OnDrag(PointerEventData eventData)
        {
            if (_markerDraggedIndex == -1 ||
                eventData.button != PointerEventData.InputButton.Left ||
                MarkerManager.EditMarkerMode != EditMarkerMode.Add)
                return;

            HandleDrag(eventData.position);

            _isDragging = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HandleStartDrag(RectTransform.ScreenToNormalizedPoint(eventData.position));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // LEFT BUTTON ONLY
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var editMarkerModeIsAdd = MarkerManager.EditMarkerMode == EditMarkerMode.Add;
            var normalizedPosition = RectTransform.ScreenToNormalizedPoint(eventData.position);

            // Si no se arrastra una distancia minima o no es legal -> Ignorar el drag
            var canDrag = editMarkerModeIsAdd &&
                          IsDraggingMarker &&
                          IsDraggedOverMinDistance(normalizedPosition) &&
                          IsLegalPos(normalizedPosition);

            if (canDrag) HandleEndDrag(normalizedPosition);
            else HandleClickWithoutDrag(normalizedPosition);

            // Fin del DRAG
            ResetDragState();
        }

        // ============================= CONDITIONS =============================
        // Dragged Marker is far enough to move
        private bool IsDraggedOverMinDistance(Vector2 normalizedPosition) =>
            MarkerManager.Markers[_markerDraggedIndex].Distance2D(normalizedPosition) > _minDragDistanceToMove;

        // LEGAL POSITION
        private bool IsLegalPos(Vector2 normalizedPosition) =>
            MapManager.Instance.IsLegalPos(normalizedPosition);

        // ============================= ACTIONS =============================

        // Simple Click
        private void HandleClickWithoutDrag(Vector2 normPos)
        {
            var anyHovered = MarkerManager.AnyHovered;
            switch (MarkerManager.EditMarkerMode)
            {
                // ELIMINAR
                case EditMarkerMode.Delete:
                    if (anyHovered)
                        MarkerManager.RemoveMarker(MarkerManager.HoveredMarkerIndex);
                    break;
                // AÑADIR
                case EditMarkerMode.Add:
                    if (!anyHovered)
                        switch (MarkerManager.SelectedCount)
                        {
                            case 0:
                                MarkerManager.AddMarker(normPos);
                                break;
                            case 1:
                                MarkerManager.MoveSelectedMarker(normPos);
                                MarkerManager.DeselectAllMarkers();
                                break;
                            case 2:
                                MarkerManager.AddMarkerBetweenSelectedPair(normPos);
                                MarkerManager.DeselectAllMarkers();
                                break;
                        }

                    break;
            }
        }

        private void HandleStartDrag(Vector2 normalizedPosition)
        {
            _markerDraggedIndex = MarkerManager.HoveredMarkerIndex;
            if (_markerDraggedIndex != -1)
                MarkerManager.ToggleSelectMarker(_markerDraggedIndex);
        }

        private void HandleEndDrag(Vector2 normalizedPosition)
        {
            MarkerManager.MoveMarker(_markerDraggedIndex, normalizedPosition);
            MarkerManager.DeselectMarker(_markerDraggedIndex);

            if (_markerPlaceholderDragged == null) return;

            if (Application.isEditor)
                DestroyImmediate(_markerPlaceholderDragged);
            else
                Destroy(_markerPlaceholderDragged);
        }

        private void HandleDrag(Vector2 mousePosition)
        {
            var localPoint = RectTransform.ScreenToLocalPoint(mousePosition);

            // Spawn Marker placeholder para que el usuario sepa que lo está moviendo
            if (_markerPlaceholderDragged == null)
                _markerPlaceholderDragged = Instantiate(
                    markerPlaceholderDraggedPrefab,
                    localPoint,
                    Quaternion.identity,
                    transform
                );
            else if (IsLegalPos(RectTransform.LocalToNormalizedPoint(localPoint)))
                _markerPlaceholderDragged.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }

        private void ResetDragState()
        {
            _markerDraggedIndex = -1;
            _isDragging = false;
        }
    }
}