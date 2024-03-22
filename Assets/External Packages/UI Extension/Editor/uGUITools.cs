/// Credit Senshi  
/// Sourced from - http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/ (uGUITools link)

using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    public static class uGUITools
    {
        [MenuItem("Tools/UnityUIExtensions/Anchors to Corners %[")]
        private static void AnchorsToCorners()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0) return;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("AnchorsToCorners");
            var undoGroup = Undo.GetCurrentGroup();

            foreach (var transform in Selection.transforms)
            {
                var t = transform as RectTransform;
                Undo.RecordObject(t, "AnchorsToCorners");
                var pt = Selection.activeTransform.parent as RectTransform;

                if (t == null || pt == null) return;

                var newAnchorsMin = new Vector2(
                    t.anchorMin.x + t.offsetMin.x / pt.rect.width,
                    t.anchorMin.y + t.offsetMin.y / pt.rect.height
                );
                var newAnchorsMax = new Vector2(
                    t.anchorMax.x + t.offsetMax.x / pt.rect.width,
                    t.anchorMax.y + t.offsetMax.y / pt.rect.height
                );

                t.anchorMin = newAnchorsMin;
                t.anchorMax = newAnchorsMax;
                t.offsetMin = t.offsetMax = new Vector2(0, 0);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("Tools/UnityUIExtensions/Corners to Anchors %]")]
        private static void CornersToAnchors()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0) return;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("CornersToAnchors");
            var undoGroup = Undo.GetCurrentGroup();

            foreach (var transform in Selection.transforms)
            {
                var t = transform as RectTransform;
                Undo.RecordObject(t, "CornersToAnchors");

                if (t == null) return;

                t.offsetMin = t.offsetMax = new Vector2(0, 0);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("Tools/UnityUIExtensions/Mirror Horizontally Around Anchors %;")]
        private static void MirrorHorizontallyAnchors()
        {
            MirrorHorizontally(false);
        }

        [MenuItem("Tools/UnityUIExtensions/Mirror Horizontally Around Parent Center %:")]
        private static void MirrorHorizontallyParent()
        {
            MirrorHorizontally(true);
        }

        private static void MirrorHorizontally(bool mirrorAnchors)
        {
            foreach (var transform in Selection.transforms)
            {
                var t = transform as RectTransform;
                var pt = Selection.activeTransform.parent as RectTransform;

                if (t == null || pt == null) return;

                if (mirrorAnchors)
                {
                    var oldAnchorMin = t.anchorMin;
                    t.anchorMin = new Vector2(1 - t.anchorMax.x, t.anchorMin.y);
                    t.anchorMax = new Vector2(1 - oldAnchorMin.x, t.anchorMax.y);
                }

                var oldOffsetMin = t.offsetMin;
                t.offsetMin = new Vector2(-t.offsetMax.x, t.offsetMin.y);
                t.offsetMax = new Vector2(-oldOffsetMin.x, t.offsetMax.y);

                t.localScale = new Vector3(-t.localScale.x, t.localScale.y, t.localScale.z);
            }
        }

        [MenuItem("Tools/UnityUIExtensions/Mirror Vertically Around Anchors %'")]
        private static void MirrorVerticallyAnchors()
        {
            MirrorVertically(false);
        }

        [MenuItem("Tools/UnityUIExtensions/Mirror Vertically Around Parent Center %\"")]
        private static void MirrorVerticallyParent()
        {
            MirrorVertically(true);
        }

        private static void MirrorVertically(bool mirrorAnchors)
        {
            foreach (var transform in Selection.transforms)
            {
                var t = transform as RectTransform;
                var pt = Selection.activeTransform.parent as RectTransform;

                if (t == null || pt == null) return;

                if (mirrorAnchors)
                {
                    var oldAnchorMin = t.anchorMin;
                    t.anchorMin = new Vector2(t.anchorMin.x, 1 - t.anchorMax.y);
                    t.anchorMax = new Vector2(t.anchorMax.x, 1 - oldAnchorMin.y);
                }

                var oldOffsetMin = t.offsetMin;
                t.offsetMin = new Vector2(t.offsetMin.x, -t.offsetMax.y);
                t.offsetMax = new Vector2(t.offsetMax.x, -oldOffsetMin.y);

                t.localScale = new Vector3(t.localScale.x, -t.localScale.y, t.localScale.z);
            }
        }
    }
}