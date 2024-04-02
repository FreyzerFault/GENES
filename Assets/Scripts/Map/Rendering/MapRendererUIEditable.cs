using DavidUtils.ExtensionMethods;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Map.Rendering
{
    public class MapRendererUIEditable
        : MapRendererUI,
            IDragHandler,
            IPointerUpHandler,
            IPointerDownHandler
    {
        [SerializeField] private GameObject markerPlaceholderDraggedPrefab;

        private readonly float _minDragDistanceToMove = 0.05f;
        private bool _isDragging;
        private int _markerDraggedIndex = -1;
        private GameObject _markerPlaceholderDragged;

        private bool IsDraggingMarker => _isDragging && _markerDraggedIndex != -1;

        private void OnEnable()
        {
            ResetDragState();
        }

        // ============================= MOUSE EVENTS =============================
        public void OnDrag(PointerEventData eventData)
        {
            if (
                _markerDraggedIndex == -1
                || eventData.button != PointerEventData.InputButton.Left
                || MarkerManager.EditMarkerMode != EditMarkerMode.Add
            )
                return;

            HandleDrag(eventData.position);

            _isDragging = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HandleStartDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // LEFT BUTTON ONLY
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var editMarkerModeIsAdd = MarkerManager.EditMarkerMode == EditMarkerMode.Add;
            var normalizedPosition = imageRectTransform.ScreenToNormalizedPoint(eventData.position);

            // Si no se arrastra una distancia minima o no es legal -> Ignorar el drag
            var canDrag =
                editMarkerModeIsAdd
                && IsDraggingMarker
                && IsDraggedOverMinDistance(normalizedPosition)
                && IsLegalPos(normalizedPosition);

            if (canDrag)
                HandleEndDrag(normalizedPosition);
            else
                HandleClickWithoutDrag(normalizedPosition);

            // Fin del DRAG
            ResetDragState();
        }

        // ============================= CONDITIONS =============================
        // Dragged Marker is far enough to move
        private bool IsDraggedOverMinDistance(Vector2 normalizedPosition) =>
            MarkerManager.Markers[_markerDraggedIndex].Distance2D(normalizedPosition)
            > _minDragDistanceToMove;

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
                case EditMarkerMode.Add:

                    if (anyHovered)
                        // Cursor sobre Marker
                    {
                        MarkerManager.ToggleSelectMarker(normPos);
                    }
                    else
                    {
                        // No hay ninguna marker en el cursor & es una Posicion LEGAL => Añadimos un marker
                        if (IsLegalPos(normPos))
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
                    }

                    break;
                case EditMarkerMode.Delete:
                    if (anyHovered) MarkerManager.RemoveMarker(MarkerManager.HoveredMarkerIndex);
                    break;
                case EditMarkerMode.Select:
                case EditMarkerMode.None:
                default:
                    break;
            }
        }

        private void HandleStartDrag()
        {
            _markerDraggedIndex = MarkerManager.HoveredMarkerIndex;
        }

        private void HandleEndDrag(Vector2 normalizedPosition)
        {
            if (!IsLegalPos(normalizedPosition)) return;
            MarkerManager.MoveMarker(_markerDraggedIndex, normalizedPosition);
            MarkerManager.DeselectMarker(_markerDraggedIndex);
        }

        private void HandleDrag(Vector2 mousePosition)
        {
            // Spawn Marker placeholder para que el usuario sepa que lo está moviendo
            if (_markerPlaceholderDragged == null)
                InstantiateMarkerPlaceholder(mousePosition);
            else
                _markerPlaceholderDragged.transform.position = mousePosition;
        }

        private void InstantiateMarkerPlaceholder(Vector2 position)
        {
            _markerPlaceholderDragged = Instantiate(
                markerPlaceholderDraggedPrefab,
                position,
                Quaternion.identity,
                imageRectTransform
            );
            _markerPlaceholderDragged.transform.localScale /= imageRectTransform.localScale.x;
        }

        private void ResetDragState()
        {
            _markerDraggedIndex = -1;
            _isDragging = false;

            if (_markerPlaceholderDragged == null) return;

            if (Application.isEditor)
                DestroyImmediate(_markerPlaceholderDragged);
            else
                Destroy(_markerPlaceholderDragged);
        }

        // ============================= DEBUG =============================

        // private void OnDrawGizmos()
        // {
        //     var imagePos = ImageRectTransform.PivotGlobal();
        //     var imageMinCorner = ImageRectTransform.MinCorner();
        //     var sizeScaled = ImageRectTransform.SizeScaled();
        //
        //     RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //         ImageRectTransform,
        //         Input.mousePosition,
        //         null,
        //         out var localPos
        //     );
        //
        //     // MIN y MAX
        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(imageMinCorner, 20);
        //     Gizmos.DrawSphere(imageMinCorner + sizeScaled, 20);
        //     Gizmos.DrawSphere(ImageRectTransform.PivotGlobal(), 20);
        //
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawSphere(imageMinCorner + localPos, 20);
        //
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(
        //         imageMinCorner + ImageRectTransform.ScreenToNormalizedPoint(Input.mousePosition) * sizeScaled, 20);
        //
        //
        //     // ESTE ES EL BUENOOOOOOOOO
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawSphere(imageMinCorner + ImageRectTransform.ScreenToLocalPoint(Input.mousePosition), 20);
        // }
    }
}