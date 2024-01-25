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
            return localPos;
        }

        public static Vector2 LocalToNormalizedPoint(this RectTransform rectT, Vector2 localPoint)
        {
            return localPoint / rectT.rect.size;
        }

        public static Vector2 ScreenToNormalizedPoint(this RectTransform rectT, Vector2 screenPos)
        {
            return rectT.LocalToNormalizedPoint(rectT.ScreenToLocalPoint(screenPos));
        }

        public static Vector2 NormalizedToLocalPoint(this RectTransform rectT, Vector2 normalizedPoint)
        {
            return normalizedPoint * rectT.rect.size;
        }
    }
}