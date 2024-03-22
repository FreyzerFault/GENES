/// Credit .entity
/// Sourced from - http://forum.unity3d.com/threads/rescale-panel.309226/

using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/RescalePanels/RescalePanel")]
    public class RescalePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Vector2 minSize;
        public Vector2 maxSize;
        private Vector2 currentPointerPosition;
        private Transform goTransform;
        private Vector2 previousPointerPosition;

        private RectTransform rectTransform;
        private Vector2 sizeDelta;

        private RectTransform thisRectTransform;

        private void Awake()
        {
            rectTransform = transform.parent.GetComponent<RectTransform>();
            goTransform = transform.parent;

            thisRectTransform = GetComponent<RectTransform>();
            sizeDelta = thisRectTransform.sizeDelta;
        }

        public void OnDrag(PointerEventData data)
        {
            if (rectTransform == null) return;

            var scaleDelta = goTransform.localScale;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                data.position,
                data.pressEventCamera,
                out currentPointerPosition
            );
            var resizeValue = currentPointerPosition - previousPointerPosition;

            scaleDelta += new Vector3(-resizeValue.y * 0.001f, -resizeValue.y * 0.001f, 0f);
            scaleDelta = new Vector3(
                Mathf.Clamp(scaleDelta.x, minSize.x, maxSize.x),
                Mathf.Clamp(scaleDelta.y, minSize.y, maxSize.y),
                1
            );

            goTransform.localScale = scaleDelta;

            previousPointerPosition = currentPointerPosition;
            var resizeDeltaValue = sizeDelta.x / goTransform.localScale.x;
            var newSizeDelta = new Vector2(resizeDeltaValue, resizeDeltaValue);
            thisRectTransform.sizeDelta = newSizeDelta;
        }

        public void OnPointerDown(PointerEventData data)
        {
            rectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                data.position,
                data.pressEventCamera,
                out previousPointerPosition
            );
        }
    }
}