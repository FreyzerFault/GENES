using UnityEngine;

namespace ExtensionMethods
{
    public static class RectTransformExtensionMethods
    {
        public static Vector2 ScreenToLocalPoint(this RectTransform rectT, Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectT,
                screenPos,
                null,
                out var localPos
            );
            return localPos * rectT.localScale + rectT.pivot * rectT.rect.size;
            // return localPos + rectT.anchoredPosition;
        }

        public static Vector2 LocalToNormalizedPoint(this RectTransform rectT, Vector2 localPoint) =>
            localPoint / (rectT.rect.size * rectT.localScale);

        public static Vector2 ScreenToNormalizedPoint(this RectTransform rectT, Vector2 screenPos) =>
            rectT.LocalToNormalizedPoint(rectT.ScreenToLocalPoint(screenPos));

        public static Vector2 NormalizedToLocalPoint(this RectTransform rectT, Vector2 normalizedPoint) =>
            normalizedPoint * rectT.rect.size * rectT.localScale;
    }
}