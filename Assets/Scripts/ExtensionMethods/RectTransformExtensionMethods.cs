using UnityEngine;

namespace ExtensionMethods
{
    public static class RectTransformExtensionMethods
    {
        // Scaled size (real en pantalla)
        public static Vector2 SizeScaled(this RectTransform rectT) => rectT.rect.size * rectT.localScale;

        // PIVOT
        public static Vector2 PivotLocal(this RectTransform rectT) => rectT.SizeScaled() * rectT.pivot;
        public static Vector2 PivotGlobal(this RectTransform rectT) => rectT.position;

        // CORNERs global positions
        public static Vector2 MinCorner(this RectTransform rectT) => (Vector2)rectT.position - rectT.PivotLocal();
        public static Vector2 MaxCorner(this RectTransform rectT) => rectT.MinCorner() + rectT.SizeScaled();

        // ============================= POINT CONVERSIONS =============================
        public static Vector2 LocalToNormalizedPoint(this RectTransform rectT, Vector2 localPoint) =>
            localPoint / (rectT.rect.size * rectT.localScale);

        public static Vector2 NormalizedToLocalPoint(this RectTransform rectT, Vector2 normalizedPoint) =>
            normalizedPoint * rectT.rect.size * rectT.localScale;

        public static Vector2 ScreenToLocalPoint(this RectTransform rectT, Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, screenPos, null, out var localPos);
            return localPos
                   // Escalado
                   * rectT.localScale
                   // Sumado al pivot para que el punto sea relativo al la esquina inferior izquierda
                   + rectT.PivotLocal();
        }

        public static Vector2 ScreenToNormalizedPoint(this RectTransform rectT, Vector2 screenPos) =>
            rectT.LocalToNormalizedPoint(rectT.ScreenToLocalPoint(screenPos));
    }
}