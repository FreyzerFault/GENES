#if UNITY_2019_1_OR_NEWER && !ENABLE_LEGACY_INPUT_MANAGER
#define NEW_INPUT_SYSTEM
#endif

using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
#if NEW_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace UnityEditor.UI
{
    /// <summary>
    ///     This script adds the Extensions UI menu options to the Unity Editor.
    /// </summary>
    internal static class ExtensionMenuOptions
    {
        #region Unity Builder section  - Do not change unless UI Source (Editor\MenuOptions) changes

        #region Unity Builder properties  - Do not change unless UI Source (Editor\MenuOptions) changes

        private const string kUILayerName = "UI";
        private const float kWidth = 160f;
        private const float kThickHeight = 30f;
        private const float kThinHeight = 20f;
        private const string kStandardSpritePath = "UI/Skin/UISprite.psd";
        private const string kBackgroundSpriteResourcePath = "UI/Skin/Background.psd";
        private const string kInputFieldBackgroundPath = "UI/Skin/InputFieldBackground.psd";
        private const string kKnobPath = "UI/Skin/Knob.psd";
        private const string kCheckmarkPath = "UI/Skin/Checkmark.psd";

        private static readonly Vector2 s_ThickGUIElementSize = new(kWidth, kThickHeight);
        private static readonly Vector2 s_ThinGUIElementSize = new(kWidth, kThinHeight);
        private static readonly Vector2 s_ImageGUIElementSize = new(100f, 100f);
        private static readonly Color s_DefaultSelectableColor = new(1f, 1f, 1f, 1f);
        private static readonly Color s_TextColor = new(50f / 255f, 50f / 255f, 50f / 255f, 1f);

        #endregion

        #region Unity Builder methods - Do not change unless UI Source (Editor\MenuOptions) changes

        private static void SetPositionVisibleinSceneView(RectTransform canvasRTransform, RectTransform itemTransform)
        {
            // Find the best scene view
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0) sceneView = SceneView.sceneViews[0] as SceneView;

            // Couldn't find a SceneView. Don't set position.
            if (sceneView == null || sceneView.camera == null) return;

            // Create world space Plane from canvas position.
            Vector2 localPlanePosition;
            var camera = sceneView.camera;
            var position = Vector3.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRTransform,
                    new Vector2(camera.pixelWidth / 2, camera.pixelHeight / 2),
                    camera,
                    out localPlanePosition
                ))
            {
                // Adjust for canvas pivot
                localPlanePosition.x = localPlanePosition.x + canvasRTransform.sizeDelta.x * canvasRTransform.pivot.x;
                localPlanePosition.y = localPlanePosition.y + canvasRTransform.sizeDelta.y * canvasRTransform.pivot.y;

                localPlanePosition.x = Mathf.Clamp(localPlanePosition.x, 0, canvasRTransform.sizeDelta.x);
                localPlanePosition.y = Mathf.Clamp(localPlanePosition.y, 0, canvasRTransform.sizeDelta.y);

                // Adjust for anchoring
                position.x = localPlanePosition.x - canvasRTransform.sizeDelta.x * itemTransform.anchorMin.x;
                position.y = localPlanePosition.y - canvasRTransform.sizeDelta.y * itemTransform.anchorMin.y;

                Vector3 minLocalPosition;
                minLocalPosition.x = canvasRTransform.sizeDelta.x * (0 - canvasRTransform.pivot.x)
                                     + itemTransform.sizeDelta.x * itemTransform.pivot.x;
                minLocalPosition.y = canvasRTransform.sizeDelta.y * (0 - canvasRTransform.pivot.y)
                                     + itemTransform.sizeDelta.y * itemTransform.pivot.y;

                Vector3 maxLocalPosition;
                maxLocalPosition.x = canvasRTransform.sizeDelta.x * (1 - canvasRTransform.pivot.x)
                                     - itemTransform.sizeDelta.x * itemTransform.pivot.x;
                maxLocalPosition.y = canvasRTransform.sizeDelta.y * (1 - canvasRTransform.pivot.y)
                                     - itemTransform.sizeDelta.y * itemTransform.pivot.y;

                position.x = Mathf.Clamp(position.x, minLocalPosition.x, maxLocalPosition.x);
                position.y = Mathf.Clamp(position.y, minLocalPosition.y, maxLocalPosition.y);
            }

            itemTransform.anchoredPosition = position;
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localScale = Vector3.one;
        }

        private static GameObject CreateUIElementRoot(string name, MenuCommand menuCommand, Vector2 size)
        {
            var parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null) parent = GetOrCreateCanvasGameObject();
            var child = new GameObject(name);

            Undo.RegisterCreatedObjectUndo(child, "Create " + name);
            Undo.SetTransformParent(child.transform, parent.transform, "Parent " + child.name);
            GameObjectUtility.SetParentAndAlign(child, parent);

            var rectTransform = child.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            if (parent != menuCommand.context) // not a context click, so center in sceneview
                SetPositionVisibleinSceneView(parent.GetComponent<RectTransform>(), rectTransform);
            Selection.activeGameObject = child;
            return child;
        }

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            GameObjectUtility.SetParentAndAlign(go, parent);
            return go;
        }

        public static void AddCanvas(MenuCommand menuCommand)
        {
            var go = CreateNewUI();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            if (go.transform.parent as RectTransform)
            {
                var rect = go.transform as RectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            Selection.activeGameObject = go;
        }

        public static GameObject CreateNewUI()
        {
            // Root for the UI
            var root = new GameObject("Canvas");
            root.layer = LayerMask.NameToLayer(kUILayerName);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);

            // if there is no event system add one...
            CreateEventSystem(false);
            return root;
        }

        public static void CreateEventSystem(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            CreateEventSystem(true, parent);
        }

        private static void CreateEventSystem(bool select)
        {
            CreateEventSystem(select, null);
        }

        private static void CreateEventSystem(bool select, GameObject parent)
        {
#if UNITY_2023_1_OR_NEWER
			var esys = Object.FindFirstObjectByType<EventSystem>();
#else
            var esys = Object.FindObjectOfType<EventSystem>();
#endif
            if (esys == null)
            {
                var eventSystem = new GameObject("EventSystem");
                GameObjectUtility.SetParentAndAlign(eventSystem, parent);
                esys = eventSystem.AddComponent<EventSystem>();
#if NEW_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif

                Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
            }

            if (select && esys != null) Selection.activeGameObject = esys.gameObject;
        }

        // Helper function that returns a Canvas GameObject; preferably a parent of the selection, or other existing Canvas.
        public static GameObject GetOrCreateCanvasGameObject()
        {
            var selectedGo = Selection.activeGameObject;

            // Try to find a gameobject that is the selected GO or one if its parents.
            var canvas = selectedGo != null ? selectedGo.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.gameObject.activeInHierarchy) return canvas.gameObject;

            // No canvas in selection or its parents? Then use just any canvas..
#if UNITY_2023_1_OR_NEWER
			canvas = Object.FindFirstObjectByType<Canvas>();
#else
            canvas = Object.FindObjectOfType(typeof(Canvas)) as Canvas;
#endif
            if (canvas != null && canvas.gameObject.activeInHierarchy) return canvas.gameObject;

            // No canvas in the scene at all? Then create a new one.
            return CreateNewUI();
        }

        private static void SetDefaultColorTransitionValues(Selectable slider)
        {
            var colors = slider.colors;
            colors.highlightedColor = new Color(0.882f, 0.882f, 0.882f);
            colors.pressedColor = new Color(0.698f, 0.698f, 0.698f);
            colors.disabledColor = new Color(0.521f, 0.521f, 0.521f);
        }

        private static void SetDefaultTextValues(Text lbl)
        {
            // Set text values we want across UI elements in default controls.
            // Don't set values which are the same as the default values for the Text component,
            // since there's no point in that, and it's good to keep them as consistent as possible.
            lbl.color = s_TextColor;
        }

        #endregion

        #endregion

        #region UI Extensions "Create" Menu items

        #region Scroll Snap controls

        [MenuItem("GameObject/UI/Extensions/Layout/Horizontal Scroll Snap", false)]
        public static void AddHorizontalScrollSnap(MenuCommand menuCommand)
        {
            var horizontalScrollSnapRoot = CreateUIElementRoot(
                "Horizontal Scroll Snap",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("Content", horizontalScrollSnapRoot);

            var childPage01 = CreateUIObject("Page_01", childContent);

            var childText = CreateUIObject("Text", childPage01);

            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = horizontalScrollSnapRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(300f, 150f);


            var image = horizontalScrollSnapRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var sr = horizontalScrollSnapRoot.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.horizontal = true;
            horizontalScrollSnapRoot.AddComponent<HorizontalScrollSnap>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;

            sr.content = rectTransformContent;

            //Setup 1st Child
            var pageImage = childPage01.AddComponent<Image>();
            pageImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            pageImage.type = Image.Type.Sliced;
            pageImage.color = s_DefaultSelectableColor;

            var rectTransformPage01 = childPage01.GetComponent<RectTransform>();
            rectTransformPage01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformPage01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformPage01.pivot = new Vector2(0f, 0.5f);

            //Setup Text on Page01
            var text = childText.AddComponent<Text>();
            text.text = "Page_01";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.196f, 0.196f, 0.196f);

            //Setup Text 1st Child
            var rectTransformPage01Text = childText.GetComponent<RectTransform>();
            rectTransformPage01Text.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformPage01Text.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformPage01Text.pivot = new Vector2(0.5f, 0.5f);


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = horizontalScrollSnapRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Layout/Vertical Scroll Snap", false)]
        public static void AddVerticallScrollSnap(MenuCommand menuCommand)
        {
            var verticalScrollSnapRoot = CreateUIElementRoot(
                "Vertical Scroll Snap",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("Content", verticalScrollSnapRoot);

            var childPage01 = CreateUIObject("Page_01", childContent);

            var childText = CreateUIObject("Text", childPage01);

            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = verticalScrollSnapRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(300f, 150f);


            var image = verticalScrollSnapRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var sr = verticalScrollSnapRoot.AddComponent<ScrollRect>();
            sr.vertical = true;
            sr.horizontal = false;
            verticalScrollSnapRoot.AddComponent<VerticalScrollSnap>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            //rectTransformContent.anchoredPosition = Vector2.zero;
            rectTransformContent.sizeDelta = Vector2.zero;

            sr.content = rectTransformContent;

            //Setup 1st Child
            var pageImage = childPage01.AddComponent<Image>();
            pageImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            pageImage.type = Image.Type.Sliced;
            pageImage.color = s_DefaultSelectableColor;

            var rectTransformPage01 = childPage01.GetComponent<RectTransform>();
            rectTransformPage01.anchorMin = new Vector2(0.5f, 0f);
            rectTransformPage01.anchorMax = new Vector2(0.5f, 0f);
            rectTransformPage01.anchoredPosition = new Vector2(
                -rectTransformPage01.sizeDelta.x / 2,
                rectTransformPage01.sizeDelta.y / 2
            );
            //rectTransformPage01.anchoredPosition = Vector2.zero;
            //rectTransformPage01.sizeDelta = Vector2.zero;
            rectTransformPage01.pivot = new Vector2(0.5f, 0f);

            //Setup Text on Page01
            var text = childText.AddComponent<Text>();
            text.text = "Page_01";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.196f, 0.196f, 0.196f);

            //Setup Text 1st Child
            var rectTransformPage01Text = childText.GetComponent<RectTransform>();
            rectTransformPage01Text.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformPage01Text.anchorMax = new Vector2(0.5f, 0.5f);
            //rectTransformPage01Text.anchoredPosition = Vector2.zero;
            //rectTransformPage01Text.sizeDelta = Vector2.zero;
            rectTransformPage01Text.pivot = new Vector2(0.5f, 0.5f);


            //Need to add example child components like in the Asset (SJ)

            Selection.activeGameObject = verticalScrollSnapRoot;
        }

        #region New ScrollSnapCode

        public static void FixedScrollSnapBase(
            MenuCommand menuCommand, string name, ScrollSnap.ScrollDirection direction, int itemVisible, int itemCount,
            Vector2 itemSize
        )
        {
            var scrollSnapRoot = CreateUIElementRoot(name, menuCommand, s_ThickGUIElementSize);
            var itemList = CreateUIObject("List", scrollSnapRoot);

            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = scrollSnapRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;

            if (direction == ScrollSnap.ScrollDirection.Horizontal)
                rectTransformScrollSnapRoot.sizeDelta = new Vector2(itemVisible * itemSize.x, itemSize.y);
            else
                rectTransformScrollSnapRoot.sizeDelta = new Vector2(itemSize.x, itemVisible * itemSize.y);

            var image = scrollSnapRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 1f);

            var listMask = scrollSnapRoot.AddComponent<Mask>();
            listMask.showMaskGraphic = false;

            var scrollRect = scrollSnapRoot.AddComponent<ScrollRect>();
            scrollRect.vertical = direction == ScrollSnap.ScrollDirection.Vertical;
            scrollRect.horizontal = direction == ScrollSnap.ScrollDirection.Horizontal;

            var scrollSnap = scrollSnapRoot.AddComponent<ScrollSnap>();
            scrollSnap.direction = direction;
            scrollSnap.ItemsVisibleAtOnce = itemVisible;

            //Setup Content container
            var rectTransformContent = itemList.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            scrollRect.content = rectTransformContent;

            //Setup Item list container
            if (direction == ScrollSnap.ScrollDirection.Horizontal)
            {
                itemList.AddComponent<HorizontalLayoutGroup>();
                var sizeFitter = itemList.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            }
            else
            {
                itemList.AddComponent<VerticalLayoutGroup>();
                var sizeFitter = itemList.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            }

            //Setup children
            for (var i = 0; i < itemCount; i++)
            {
                var item = CreateUIObject(string.Format("Item_{0:00}", i), itemList);
                var childText = CreateUIObject("Text", item);

                var pageImage = item.AddComponent<Image>();
                pageImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
                pageImage.type = Image.Type.Sliced;
                pageImage.color = s_DefaultSelectableColor;

                var elementLayout = item.AddComponent<LayoutElement>();
                if (direction == ScrollSnap.ScrollDirection.Horizontal)
                    elementLayout.minWidth = itemSize.x;
                else
                    elementLayout.minHeight = itemSize.y;

                var rectTransformPage01 = item.GetComponent<RectTransform>();
                rectTransformPage01.anchorMin = new Vector2(0f, 0.5f);
                rectTransformPage01.anchorMax = new Vector2(0f, 0.5f);
                rectTransformPage01.pivot = new Vector2(0f, 0.5f);

                //Setup Text on Page01
                var text = childText.AddComponent<Text>();
                text.text = item.name;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.196f, 0.196f, 0.196f);

                //Setup Text 1st Child
                var rectTransformPage01Text = childText.GetComponent<RectTransform>();
                rectTransformPage01Text.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransformPage01Text.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransformPage01Text.pivot = new Vector2(0.5f, 0.5f);
            }

            Selection.activeGameObject = scrollSnapRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Fixed Item Scroll/Snap Horizontal Single Item", false)]
        public static void AddFixedItemScrollSnapHorizontalSingle(MenuCommand menuCommand)
        {
            FixedScrollSnapBase(
                menuCommand,
                "Scroll Snap Horizontal Single",
                ScrollSnap.ScrollDirection.Horizontal,
                1,
                3,
                new Vector2(100, 100)
            );
        }

        [MenuItem("GameObject/UI/Extensions/Fixed Item Scroll/Snap Horizontal Multiple Items", false)]
        public static void AddFixedItemScrollSnapHorizontalMultiple(MenuCommand menuCommand)
        {
            FixedScrollSnapBase(
                menuCommand,
                "Scroll Snap Horizontal Multiple",
                ScrollSnap.ScrollDirection.Horizontal,
                3,
                15,
                new Vector2(100, 100)
            );
        }

        [MenuItem("GameObject/UI/Extensions/Fixed Item Scroll/Snap Vertical Single Item", false)]
        public static void AddFixedItemScrollSnapVerticalSingle(MenuCommand menuCommand)
        {
            FixedScrollSnapBase(
                menuCommand,
                "Scroll Snap Vertical Multiple",
                ScrollSnap.ScrollDirection.Vertical,
                1,
                3,
                new Vector2(100, 100)
            );
        }

        [MenuItem("GameObject/UI/Extensions/Fixed Item Scroll/Snap Vertical Multiple Items", false)]
        public static void AddFixedItemScrollSnapVerticalMultiple(MenuCommand menuCommand)
        {
            FixedScrollSnapBase(
                menuCommand,
                "Scroll Snap Vertical Multiple",
                ScrollSnap.ScrollDirection.Vertical,
                3,
                15,
                new Vector2(100, 100)
            );
        }

        #endregion

        #region ContentScrollSnapHorizontal

        [MenuItem("GameObject/UI/Extensions/Layout/Content Scroll Snap Horizontal", false)]
        public static void AddContentScrollSnapHorizontal(MenuCommand menuCommand)
        {
            var contentScrollSnapRoot = CreateUIElementRoot(
                "Content Scroll Snap Horizontal",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("Content", contentScrollSnapRoot);

            var childPage01 = CreateUIObject("Position 1", childContent);

            var childPage02 = CreateUIObject("Position 2", childContent);

            var childPage03 = CreateUIObject("Position 3", childContent);

            var childPage04 = CreateUIObject("Position 4", childContent);

            var childPage05 = CreateUIObject("Position 5", childContent);

            //setup root
            var contentScrollSnapRectTransform = (RectTransform)contentScrollSnapRoot.transform;
            contentScrollSnapRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            contentScrollSnapRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            contentScrollSnapRectTransform.anchoredPosition = Vector2.zero;
            contentScrollSnapRectTransform.sizeDelta = new Vector2(100, 200);

            var image = contentScrollSnapRoot.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(1, 0, 0, .5f);

            var sr = contentScrollSnapRoot.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.horizontal = true;

            //setup content container
            var contentTransform = (RectTransform)childContent.transform;
            contentTransform.anchorMin = new Vector2(.5f, .5f);
            contentTransform.anchorMax = new Vector2(.5f, .5f);
            contentTransform.pivot = new Vector2(.5f, .5f);
            contentTransform.sizeDelta = new Vector2(200, 300);

            var contentImage = childContent.AddComponent<Image>();
            contentImage.sprite = null;
            contentImage.color = new Color(0, 0, 1, .5f);

            sr.content = contentTransform;

            //setup child 1
            var childPage01Transform = (RectTransform)childPage01.transform;
            childPage01Transform.anchorMin = new Vector2(0, 1);
            childPage01Transform.anchorMax = new Vector2(0, 1);
            childPage01Transform.pivot = new Vector2(0, 1);
            childPage01Transform.anchoredPosition = new Vector2(0, -125);

            var childPage01Image = childPage01.AddComponent<Image>();
            childPage01Image.sprite = null;
            childPage01Image.color = Color.white;

            //setup child 2
            var childPage02Transform = (RectTransform)childPage02.transform;
            childPage02Transform.anchorMin = new Vector2(0, 1);
            childPage02Transform.anchorMax = new Vector2(0, 1);
            childPage02Transform.pivot = new Vector2(0, 1);
            childPage02Transform.anchoredPosition = new Vector2(175, -150);

            var childPage02Image = childPage02.AddComponent<Image>();
            childPage02Image.sprite = null;
            childPage02Image.color = Color.white;

            //setup child 3
            var childPage03Transform = (RectTransform)childPage03.transform;
            childPage03Transform.anchorMin = new Vector2(0, 1);
            childPage03Transform.anchorMax = new Vector2(0, 1);
            childPage03Transform.pivot = new Vector2(0, 1);
            childPage03Transform.anchoredPosition = new Vector2(315, -125);
            childPage03Transform.sizeDelta = new Vector2(50, 100);

            var childPage03Image = childPage03.AddComponent<Image>();
            childPage03Image.sprite = null;
            childPage03Image.color = Color.white;

            //setup child 4
            var childPage04Transform = (RectTransform)childPage04.transform;
            childPage04Transform.anchorMin = new Vector2(0, 1);
            childPage04Transform.anchorMax = new Vector2(0, 1);
            childPage04Transform.pivot = new Vector2(0, 1);
            childPage04Transform.anchoredPosition = new Vector2(490, -110);

            var childPage04Image = childPage04.AddComponent<Image>();
            childPage04Image.sprite = null;
            childPage04Image.color = Color.white;

            //setup child 5
            var childPage05Transform = (RectTransform)childPage05.transform;
            childPage05Transform.anchorMin = new Vector2(0, 1);
            childPage05Transform.anchorMax = new Vector2(0, 1);
            childPage05Transform.pivot = new Vector2(0, 1);
            childPage05Transform.anchoredPosition = new Vector2(630, -180);

            var childPage05Image = childPage05.AddComponent<Image>();
            childPage05Image.sprite = null;
            childPage05Image.color = Color.white;

            //add scroll snap after we've added the content & items
            contentScrollSnapRoot.AddComponent<ContentScrollSnapHorizontal>();
        }

        #endregion

        #endregion

        #region UIVertical Scroller

        [MenuItem("GameObject/UI/Extensions/Layout/UI Vertical Scroller", false)]
        public static void AddUIVerticallScroller(MenuCommand menuCommand)
        {
            var uiVerticalScrollerRoot = CreateUIElementRoot(
                "UI Vertical Scroller",
                menuCommand,
                s_ThickGUIElementSize
            );

            var uiScrollerCenter = CreateUIObject("Center", uiVerticalScrollerRoot);

            var childContent = CreateUIObject("Content", uiVerticalScrollerRoot);

            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = uiVerticalScrollerRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(500f, 150f);

            // Add required ScrollRect
            var sr = uiVerticalScrollerRoot.AddComponent<ScrollRect>();
            sr.vertical = true;
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Unrestricted;
            var uiscr = uiVerticalScrollerRoot.AddComponent<UIVerticalScroller>();

            //Setup container center point
            var rectTransformCenter = uiScrollerCenter.GetComponent<RectTransform>();
            rectTransformCenter.anchorMin = new Vector2(0f, 0.3f);
            rectTransformCenter.anchorMax = new Vector2(1f, 0.6f);
            rectTransformCenter.sizeDelta = Vector2.zero;

            uiscr.Center = uiScrollerCenter.GetComponent<RectTransform>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;

            sr.content = rectTransformContent;

            // Add sample children
            for (var i = 0; i < 10; i++)
            {
                var childPage = CreateUIObject("Page_" + i, childContent);

                var childText = CreateUIObject("Text", childPage);

                //Setup 1st Child
                var pageImage = childPage.AddComponent<Image>();
                pageImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
                pageImage.type = Image.Type.Sliced;
                pageImage.color = s_DefaultSelectableColor;

                var rectTransformPage = childPage.GetComponent<RectTransform>();
                rectTransformPage.anchorMin = new Vector2(0f, 0.5f);
                rectTransformPage.anchorMax = new Vector2(1f, 0.5f);
                rectTransformPage.sizeDelta = new Vector2(0f, 80f);
                rectTransformPage.pivot = new Vector2(0.5f, 0.5f);
                rectTransformPage.localPosition = new Vector3(0, 80 * i, 0);
                childPage.AddComponent<Button>();

                var childCG = childPage.AddComponent<CanvasGroup>();
                childCG.interactable = false;

                //Setup Text on Item
                var text = childText.AddComponent<Text>();
                text.text = "Item_" + i;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.196f, 0.196f, 0.196f);

                //Setup Text on Item
                var rectTransformPageText = childText.GetComponent<RectTransform>();
                rectTransformPageText.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransformPageText.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransformPageText.pivot = new Vector2(0.5f, 0.5f);
            }

            Selection.activeGameObject = uiVerticalScrollerRoot;
        }

        #endregion

        #region UIHorizontal Scroller

        [MenuItem("GameObject/UI/Extensions/Layout/UI Horizontal Scroller", false)]
        public static void AddUIHorizontalScroller(MenuCommand menuCommand)
        {
            var uiHorizontalScrollerRoot = CreateUIElementRoot(
                "UI Horizontal Scroller",
                menuCommand,
                s_ThickGUIElementSize
            );

            var uiScrollerCenter = CreateUIObject("Center", uiHorizontalScrollerRoot);

            var childContent = CreateUIObject("Content", uiHorizontalScrollerRoot);

            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = uiHorizontalScrollerRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(500f, 150f);

            // Add required ScrollRect
            var sr = uiHorizontalScrollerRoot.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.horizontal = true;
            sr.movementType = ScrollRect.MovementType.Unrestricted;
            var uiscr = uiHorizontalScrollerRoot.AddComponent<UIHorizontalScroller>();

            //Setup container center point
            var rectTransformCenter = uiScrollerCenter.GetComponent<RectTransform>();
            rectTransformCenter.anchorMin = new Vector2(0.3f, 0f);
            rectTransformCenter.anchorMax = new Vector2(0.6f, 1f);
            rectTransformCenter.sizeDelta = Vector2.zero;

            uiscr.Center = uiScrollerCenter.GetComponent<RectTransform>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;

            sr.content = rectTransformContent;

            // Add sample children
            for (var i = 0; i < 10; i++)
            {
                var childPage = CreateUIObject("Page_" + i, childContent);

                var childText = CreateUIObject("Text", childPage);

                //Setup 1st Child
                var pageImage = childPage.AddComponent<Image>();
                pageImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
                pageImage.type = Image.Type.Sliced;
                pageImage.color = s_DefaultSelectableColor;

                var rectTransformPage = childPage.GetComponent<RectTransform>();
                rectTransformPage.anchorMin = new Vector2(0.5f, 0);
                rectTransformPage.anchorMax = new Vector2(0.5f, 1f);
                rectTransformPage.sizeDelta = new Vector2(80f, 0f);
                rectTransformPage.pivot = new Vector2(0.5f, 0.5f);
                rectTransformPage.localPosition = new Vector3(80 * i, 0, 0);
                childPage.AddComponent<Button>();

                var childCG = childPage.AddComponent<CanvasGroup>();
                childCG.interactable = false;

                //Setup Text on Item
                var text = childText.AddComponent<Text>();
                text.text = "Item_" + i;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.196f, 0.196f, 0.196f);

                //Setup Text on Item
                var rectTransformPageText = childText.GetComponent<RectTransform>();
                rectTransformPageText.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransformPageText.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransformPageText.pivot = new Vector2(0.5f, 0.5f);
            }

            Selection.activeGameObject = uiHorizontalScrollerRoot;
        }

        #endregion

        #region UI Button

        [MenuItem("GameObject/UI/Extensions/Controls/UI Button", false)]
        public static void AddUIButton(MenuCommand menuCommand)
        {
            var uiButtonRoot = CreateUIElementRoot("UI Button", menuCommand, s_ThickGUIElementSize);
            var childText = CreateUIObject("Text", uiButtonRoot);

            var image = uiButtonRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            image.type = Image.Type.Sliced;
            image.color = s_DefaultSelectableColor;

            var bt = uiButtonRoot.AddComponent<Button>();
            uiButtonRoot.AddComponent<UISelectableExtension>();
            SetDefaultColorTransitionValues(bt);

            var text = childText.AddComponent<Text>();
            text.text = "Button";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.196f, 0.196f, 0.196f);

            var textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;

            Selection.activeGameObject = uiButtonRoot;
        }

        #endregion

        #region UI Flippable

        [MenuItem("GameObject/UI/Extensions/Controls/UI Flippable", false)]
        public static void AddUIFlippableImage(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Flippable", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<Image>();
            go.AddComponent<UIFlippable>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region UI WindowBase

        [MenuItem("GameObject/UI/Extensions/Controls/UI Window Base", false)]
        public static void AddUIWindowBase(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Window Base", menuCommand, s_ThickGUIElementSize);
            go.AddComponent<UIWindowBase>();
            go.AddComponent<Image>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region Accordion

        [MenuItem("GameObject/UI/Extensions/Accordion/Accordion", false)]
        public static void AddAccordionVertical(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Accordion Group", menuCommand, s_ThickGUIElementSize);
            CreateAccordionGroup(go);
            for (var i = 0; i < 3; i++)
            {
                var child = CreateUIObject($"Accordion Element {i}", go);
                CreateAccordionElement(child);
            }

            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Accordion/Accordion Group", false)]
        public static void AddAccordionGroup(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Accordion Group", menuCommand, s_ThickGUIElementSize);
            CreateAccordionGroup(go);
            Selection.activeGameObject = go;
        }

        private static void CreateAccordionGroup(GameObject go)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandHeight = true;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<ToggleGroup>();
            go.AddComponent<Accordion>();
        }

        [MenuItem("GameObject/UI/Extensions/Accordion/Accordion Element", false)]
        public static void AddAccordionElement(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Accordion Element", menuCommand, s_ThickGUIElementSize);
            CreateAccordionElement(go);

            Selection.activeGameObject = go;
        }

        private static void CreateAccordionElement(GameObject go)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandHeight = true;
            go.AddComponent<LayoutElement>();
            var accordionElement = go.AddComponent<AccordionElement>();

            // Header
            var headergo = CreateUIObject("Header", go);
            var headerLayout = headergo.AddComponent<LayoutElement>();
            headerLayout.minHeight = accordionElement.MinHeight;
            var headerText = headergo.AddComponent<Text>();
            headerText.text = "This is an Accordion header";

            // Text
            var textgo = CreateUIObject("Text", go);
            var textText = textgo.AddComponent<Text>();
            textText.text =
                "This is an example of text in an accordion element\nLots of information can be put here for selection\nIf you like";
        }

        #endregion

        #region Drop Down controls

        [MenuItem("GameObject/UI/Extensions/ComboBox/AutoComplete ComboBox", false)]
        public static void AddAutoCompleteComboBox(MenuCommand menuCommand)
        {
            var autoCompleteComboBoxRoot = CreateUIElementRoot(
                "AutoCompleteComboBox",
                menuCommand,
                s_ThickGUIElementSize
            );

            //Create Template
            var itemTemplate = AddButtonAsChild(autoCompleteComboBoxRoot);

            //Create Inputfield
            var inputField = AddInputFieldAsChild(autoCompleteComboBoxRoot);

            //Create Overlay
            var overlay = CreateUIObject("Overlay", autoCompleteComboBoxRoot);
            var overlayScrollPanel = CreateUIObject("ScrollPanel", overlay);
            var overlayScrollPanelItems = CreateUIObject("Items", overlayScrollPanel);
            var overlayScrollPanelScrollBar = AddScrollbarAsChild(overlayScrollPanel);

            //Create Arrow Button
            var arrowButton = AddButtonAsChild(autoCompleteComboBoxRoot);

            //Setup ComboBox
            var autoCompleteComboBox = autoCompleteComboBoxRoot.AddComponent<AutoCompleteComboBox>();
            var cbbRT = autoCompleteComboBoxRoot.GetComponent<RectTransform>();

            //Setup Template
            itemTemplate.name = "ItemTemplate";
            var itemTemplateRT = itemTemplate.GetComponent<RectTransform>();
            itemTemplateRT.sizeDelta = cbbRT.sizeDelta - new Vector2(10, 0);
            itemTemplateRT.anchoredPosition = new Vector2(-5, 0);
            var itemTemplateButton = itemTemplate.GetComponent<Button>();
            itemTemplateButton.transition = Selectable.Transition.None;
            var itemTemplateLayoutElement = itemTemplate.AddComponent<LayoutElement>();
            itemTemplateLayoutElement.minHeight = cbbRT.rect.height;
            itemTemplate.SetActive(false);

            //Setup InputField
            var inputFieldRT = inputField.GetComponent<RectTransform>();
            inputFieldRT.anchorMin = Vector2.zero;
            inputFieldRT.anchorMax = Vector2.one;
            inputFieldRT.sizeDelta = Vector2.zero;
            UnityEventTools.AddPersistentListener(
                inputField.GetComponent<InputField>().onValueChanged,
                autoCompleteComboBox.OnValueChanged
            );

            //Setup Overlay
            var overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = new Vector2(0f, 1f);
            overlayRT.anchorMax = new Vector2(0f, 1f);
            overlayRT.sizeDelta = new Vector2(0f, 1f);
            overlayRT.pivot = new Vector2(0f, 1f);
            overlay.AddComponent<Image>().color = new Color(0.243f, 0.871f, 0f, 0f);
            UnityEventTools.AddBoolPersistentListener(
                overlay.AddComponent<Button>().onClick,
                autoCompleteComboBox.ToggleDropdownPanel,
                true
            );
            //Overlay Scroll Panel
            var overlayScrollPanelRT = overlayScrollPanel.GetComponent<RectTransform>();
            overlayScrollPanelRT.position += new Vector3(0, -cbbRT.sizeDelta.y, 0);
            overlayScrollPanelRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelRT.sizeDelta = new Vector2(cbbRT.sizeDelta.x, cbbRT.sizeDelta.y * 3);
            overlayScrollPanelRT.pivot = new Vector2(0f, 1f);
            overlayScrollPanel.AddComponent<Image>();
            overlayScrollPanel.AddComponent<Mask>();
            var scrollRect = overlayScrollPanel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = overlayScrollPanelScrollBar.GetComponent<Scrollbar>();
            //Overlay Items list
            var overlayScrollPanelItemsRT = overlayScrollPanelItems.GetComponent<RectTransform>();
            overlayScrollPanelItemsRT.position += new Vector3(5, 0, 0);
            overlayScrollPanelItemsRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.sizeDelta = new Vector2(120f, 5f);
            overlayScrollPanelItemsRT.pivot = new Vector2(0f, 1f);
            scrollRect.content = overlayScrollPanelItemsRT;
            var overlayScrollPanelItemsVLG = overlayScrollPanelItems.AddComponent<VerticalLayoutGroup>();
            overlayScrollPanelItemsVLG.padding = new RectOffset(0, 0, 5, 0);
            var overlayScrollPanelItemsFitter = overlayScrollPanelItems.AddComponent<ContentSizeFitter>();
            overlayScrollPanelItemsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            //Overlay Scrollbar
            var overlayScrollPanelScrollbarRT = overlayScrollPanelScrollBar.GetComponent<RectTransform>();
            overlayScrollPanelScrollbarRT.anchorMin = new Vector2(1f, 0f);
            overlayScrollPanelScrollbarRT.anchorMax = Vector2.one;
            overlayScrollPanelScrollbarRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, 0f);
            overlayScrollPanelScrollbarRT.pivot = Vector2.one;
            overlayScrollPanelScrollbarRT.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
            overlayScrollPanelScrollBar.transform.GetChild(0).name = "SlidingArea";

            //Arrow Button
            arrowButton.name = "ArrowBtn";
            var arrowButtonRT = arrowButton.GetComponent<RectTransform>();
            arrowButtonRT.anchorMin = Vector2.one;
            arrowButtonRT.anchorMax = Vector2.one;
            arrowButtonRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, cbbRT.sizeDelta.y);
            arrowButtonRT.pivot = Vector2.one;
            UnityEventTools.AddBoolPersistentListener(
                arrowButton.GetComponent<Button>().onClick,
                autoCompleteComboBox.ToggleDropdownPanel,
                true
            );
            arrowButton.GetComponentInChildren<Text>().text = "▼";

            Selection.activeGameObject = autoCompleteComboBoxRoot;
        }

        [MenuItem("GameObject/UI/Extensions/ComboBox/ComboBox", false)]
        public static void AddComboBox(MenuCommand menuCommand)
        {
            var comboBoxRoot = CreateUIElementRoot("ComboBox", menuCommand, s_ThickGUIElementSize);

            //Create Template
            var itemTemplate = AddButtonAsChild(comboBoxRoot);

            //Create Inputfield
            var inputField = AddInputFieldAsChild(comboBoxRoot);

            //Create Overlay
            var overlay = CreateUIObject("Overlay", comboBoxRoot);
            var overlayScrollPanel = CreateUIObject("ScrollPanel", overlay);
            var overlayScrollPanelItems = CreateUIObject("Items", overlayScrollPanel);
            var overlayScrollPanelScrollBar = AddScrollbarAsChild(overlayScrollPanel);

            //Create Arrow Button
            var arrowButton = AddButtonAsChild(comboBoxRoot);

            //Setup ComboBox
            var comboBox = comboBoxRoot.AddComponent<ComboBox>();
            var cbbRT = comboBoxRoot.GetComponent<RectTransform>();

            //Setup Template
            itemTemplate.name = "ItemTemplate";
            var itemTemplateRT = itemTemplate.GetComponent<RectTransform>();
            itemTemplateRT.sizeDelta = cbbRT.sizeDelta - new Vector2(10, 0);
            itemTemplateRT.anchoredPosition = new Vector2(-5, 0);
            var itemTemplateButton = itemTemplate.GetComponent<Button>();
            itemTemplateButton.transition = Selectable.Transition.None;
            var itemTemplateLayoutElement = itemTemplate.AddComponent<LayoutElement>();
            itemTemplateLayoutElement.minHeight = cbbRT.rect.height;
            itemTemplate.SetActive(false);

            //Setup InputField
            var inputFieldRT = inputField.GetComponent<RectTransform>();
            inputFieldRT.anchorMin = Vector2.zero;
            inputFieldRT.anchorMax = Vector2.one;
            inputFieldRT.sizeDelta = Vector2.zero;
            UnityEventTools.AddPersistentListener(
                inputField.GetComponent<InputField>().onValueChanged,
                comboBox.OnValueChanged
            );

            //Setup Overlay
            var overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = new Vector2(0f, 1f);
            overlayRT.anchorMax = new Vector2(0f, 1f);
            overlayRT.sizeDelta = new Vector2(0f, 1f);
            overlayRT.pivot = new Vector2(0f, 1f);
            overlay.AddComponent<Image>().color = new Color(0.243f, 0.871f, 0f, 0f);
            UnityEventTools.AddBoolPersistentListener(
                overlay.AddComponent<Button>().onClick,
                comboBox.ToggleDropdownPanel,
                true
            );
            //Overlay Scroll Panel
            var overlayScrollPanelRT = overlayScrollPanel.GetComponent<RectTransform>();
            overlayScrollPanelRT.position += new Vector3(0, -cbbRT.sizeDelta.y, 0);
            overlayScrollPanelRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelRT.sizeDelta = new Vector2(cbbRT.sizeDelta.x, cbbRT.sizeDelta.y * 3);
            overlayScrollPanelRT.pivot = new Vector2(0f, 1f);
            overlayScrollPanel.AddComponent<Image>();
            overlayScrollPanel.AddComponent<Mask>();
            var scrollRect = overlayScrollPanel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = overlayScrollPanelScrollBar.GetComponent<Scrollbar>();
            //Overlay Items list
            var overlayScrollPanelItemsRT = overlayScrollPanelItems.GetComponent<RectTransform>();
            overlayScrollPanelItemsRT.position += new Vector3(5, 0, 0);
            overlayScrollPanelItemsRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.sizeDelta = new Vector2(120f, 5f);
            overlayScrollPanelItemsRT.pivot = new Vector2(0f, 1f);
            scrollRect.content = overlayScrollPanelItemsRT;
            var overlayScrollPanelItemsVLG = overlayScrollPanelItems.AddComponent<VerticalLayoutGroup>();
            overlayScrollPanelItemsVLG.padding = new RectOffset(0, 0, 5, 0);
            var overlayScrollPanelItemsFitter = overlayScrollPanelItems.AddComponent<ContentSizeFitter>();
            overlayScrollPanelItemsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            //Overlay Scrollbar
            var overlayScrollPanelScrollbarRT = overlayScrollPanelScrollBar.GetComponent<RectTransform>();
            overlayScrollPanelScrollbarRT.anchorMin = new Vector2(1f, 0f);
            overlayScrollPanelScrollbarRT.anchorMax = Vector2.one;
            overlayScrollPanelScrollbarRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, 0f);
            overlayScrollPanelScrollbarRT.pivot = Vector2.one;
            overlayScrollPanelScrollbarRT.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
            overlayScrollPanelScrollBar.transform.GetChild(0).name = "SlidingArea";

            //Arrow Button
            arrowButton.name = "ArrowBtn";
            var arrowButtonRT = arrowButton.GetComponent<RectTransform>();
            arrowButtonRT.anchorMin = Vector2.one;
            arrowButtonRT.anchorMax = Vector2.one;
            arrowButtonRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, cbbRT.sizeDelta.y);
            arrowButtonRT.pivot = Vector2.one;
            UnityEventTools.AddBoolPersistentListener(
                arrowButton.GetComponent<Button>().onClick,
                comboBox.ToggleDropdownPanel,
                true
            );
            arrowButton.GetComponentInChildren<Text>().text = "▼";

            Selection.activeGameObject = comboBoxRoot;
        }

        [MenuItem("GameObject/UI/Extensions/ComboBox/DropDownList", false)]
        public static void AddDropDownList(MenuCommand menuCommand)
        {
            var dropDownListRoot = CreateUIElementRoot("DropDownList", menuCommand, s_ThickGUIElementSize);

            //Create Template
            var itemTemplate = AddButtonAsChild(dropDownListRoot);
            var itemTemplateImage = AddImageAsChild(itemTemplate);
            itemTemplateImage.GetComponent<Transform>().SetSiblingIndex(0);

            //Create Main Button
            var mainButton = AddButtonAsChild(dropDownListRoot);
            var mainButtonImage = AddImageAsChild(mainButton);
            mainButtonImage.GetComponent<Transform>().SetSiblingIndex(0);

            //Create Overlay
            var overlay = CreateUIObject("Overlay", dropDownListRoot);
            var overlayScrollPanel = CreateUIObject("ScrollPanel", overlay);
            var overlayScrollPanelItems = CreateUIObject("Items", overlayScrollPanel);
            var overlayScrollPanelScrollBar = AddScrollbarAsChild(overlayScrollPanel);

            //Create Arrow Button
            var arrowText = AddTextAsChild(dropDownListRoot);

            //Setup DropDownList
            var dropDownList = dropDownListRoot.AddComponent<DropDownList>();
            var cbbRT = dropDownListRoot.GetComponent<RectTransform>();

            //Setup Template
            itemTemplate.name = "ItemTemplate";
            var itemTemplateRT = itemTemplate.GetComponent<RectTransform>();
            itemTemplateRT.sizeDelta = cbbRT.sizeDelta - new Vector2(10, 0);
            itemTemplateRT.anchoredPosition = new Vector2(-5, 0);
            var itemTemplateButton = itemTemplate.GetComponent<Button>();
            itemTemplateButton.transition = Selectable.Transition.None;
            var itemTemplateLayoutElement = itemTemplate.AddComponent<LayoutElement>();
            itemTemplateLayoutElement.minHeight = cbbRT.rect.height;
            itemTemplate.SetActive(false);
            //Item Template Image
            var itemTemplateImageRT = itemTemplateImage.GetComponent<RectTransform>();
            itemTemplateImageRT.anchorMin = Vector2.zero;
            itemTemplateImageRT.anchorMax = new Vector2(0f, 1f);
            itemTemplateImageRT.pivot = new Vector2(0f, 1f);
            itemTemplateImageRT.sizeDelta = Vector2.one;
            itemTemplateImageRT.offsetMin = new Vector2(4f, 4f);
            itemTemplateImageRT.offsetMax = new Vector2(22f, -4f);
            itemTemplateImage.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            //Setup Main Button
            mainButton.name = "MainButton";
            var mainButtonRT = mainButton.GetComponent<RectTransform>();
            mainButtonRT.anchorMin = Vector2.zero;
            mainButtonRT.anchorMax = Vector2.one;
            mainButtonRT.sizeDelta = Vector2.zero;
            UnityEventTools.AddPersistentListener(
                mainButton.GetComponent<Button>().onClick,
                dropDownList.ToggleDropdownPanel
            );
            var mainButtonText = mainButton.GetComponentInChildren<Text>();
            mainButtonText.alignment = TextAnchor.MiddleLeft;
            mainButtonText.text = "Select Item...";
            var mainButtonTextRT = mainButtonText.GetComponent<RectTransform>();
            mainButtonTextRT.anchorMin = Vector2.zero;
            mainButtonTextRT.anchorMin = Vector2.zero;
            mainButtonTextRT.pivot = new Vector2(0f, 1f);
            mainButtonTextRT.offsetMin = new Vector2(10f, 0f);
            mainButtonTextRT.offsetMax = new Vector2(-4f, 0f);
            //Main Button Image
            var mainButtonImageRT = mainButtonImage.GetComponent<RectTransform>();
            mainButtonImageRT.anchorMin = Vector2.zero;
            mainButtonImageRT.anchorMax = new Vector2(0f, 1f);
            mainButtonImageRT.pivot = new Vector2(0f, 1f);
            mainButtonImageRT.sizeDelta = Vector2.one;
            mainButtonImageRT.offsetMin = new Vector2(4f, 4f);
            mainButtonImageRT.offsetMax = new Vector2(22f, -4f);
            mainButtonImageRT.GetComponent<Image>().color = new Color(1, 1, 1, 0);


            //Setup Overlay
            var overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = new Vector2(0f, 1f);
            overlayRT.anchorMax = new Vector2(0f, 1f);
            overlayRT.sizeDelta = new Vector2(0f, 1f);
            overlayRT.pivot = new Vector2(0f, 1f);
            overlay.AddComponent<Image>().color = new Color(0.243f, 0.871f, 0f, 0f);
            UnityEventTools.AddPersistentListener(
                overlay.AddComponent<Button>().onClick,
                dropDownList.ToggleDropdownPanel
            );
            //Overlay Scroll Panel
            var overlayScrollPanelRT = overlayScrollPanel.GetComponent<RectTransform>();
            overlayScrollPanelRT.position += new Vector3(0, -cbbRT.sizeDelta.y, 0);
            overlayScrollPanelRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelRT.sizeDelta = new Vector2(cbbRT.sizeDelta.x, cbbRT.sizeDelta.y * 3);
            overlayScrollPanelRT.pivot = new Vector2(0f, 1f);
            overlayScrollPanel.AddComponent<Image>();
            overlayScrollPanel.AddComponent<Mask>();
            var scrollRect = overlayScrollPanel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = overlayScrollPanelScrollBar.GetComponent<Scrollbar>();
            //Overlay Items list
            var overlayScrollPanelItemsRT = overlayScrollPanelItems.GetComponent<RectTransform>();
            overlayScrollPanelItemsRT.position += new Vector3(5, 0, 0);
            overlayScrollPanelItemsRT.anchorMin = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.anchorMax = new Vector2(0f, 1f);
            overlayScrollPanelItemsRT.sizeDelta = new Vector2(120f, 5f);
            overlayScrollPanelItemsRT.pivot = new Vector2(0f, 1f);
            scrollRect.content = overlayScrollPanelItemsRT;
            var overlayScrollPanelItemsVLG = overlayScrollPanelItems.AddComponent<VerticalLayoutGroup>();
            overlayScrollPanelItemsVLG.padding = new RectOffset(0, 0, 5, 0);
            var overlayScrollPanelItemsFitter = overlayScrollPanelItems.AddComponent<ContentSizeFitter>();
            overlayScrollPanelItemsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            //Overlay Scrollbar
            var overlayScrollPanelScrollbarRT = overlayScrollPanelScrollBar.GetComponent<RectTransform>();
            overlayScrollPanelScrollbarRT.anchorMin = new Vector2(1f, 0f);
            overlayScrollPanelScrollbarRT.anchorMax = Vector2.one;
            overlayScrollPanelScrollbarRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, 0f);
            overlayScrollPanelScrollbarRT.pivot = Vector2.one;
            overlayScrollPanelScrollbarRT.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
            overlayScrollPanelScrollBar.transform.GetChild(0).name = "SlidingArea";

            //Arrow Button
            arrowText.name = "Arrow";
            var arrowTextRT = arrowText.GetComponent<RectTransform>();
            arrowTextRT.anchorMin = new Vector2(1f, 0f);
            arrowTextRT.anchorMax = Vector2.one;
            arrowTextRT.sizeDelta = new Vector2(cbbRT.sizeDelta.y, cbbRT.sizeDelta.y);
            arrowTextRT.pivot = new Vector2(1f, 0.5f);
            var arrowTextComponent = arrowText.GetComponent<Text>();
            arrowTextComponent.text = "▼";
            arrowTextComponent.alignment = TextAnchor.MiddleCenter;
            var arrowTextCanvasGroup = arrowText.AddComponent<CanvasGroup>();
            arrowTextCanvasGroup.interactable = false;
            arrowTextCanvasGroup.blocksRaycasts = false;
            Selection.activeGameObject = dropDownListRoot;
        }

        #endregion

        #region RTS Selection box

        [MenuItem("GameObject/UI/Extensions/Selection Box", false)]
        public static void AddSelectionBox(MenuCommand menuCommand)
        {
            var go = CreateNewUI();
            go.name = "Selection Box";
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            var rect = go.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.color = new Color(1f, 1f, 1f, 0.392f);


            var selectableArea = go.AddComponent<SelectionBox>();
            selectableArea.selectionMask = rect;
            selectableArea.color = new Color(0.816f, 0.816f, 0.816f, 0.353f);


            var childSelectableItem = CreateUIObject("Selectable", go);
            childSelectableItem.AddComponent<Image>();
            childSelectableItem.AddComponent<ExampleSelectable>();


            Selection.activeGameObject = go;
        }

        #endregion

        #region Bound Tooltip

        [MenuItem("GameObject/UI/Extensions/Bound Tooltip/Tooltip", false)]
        public static void AddBoundTooltip(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Tooltip", menuCommand, s_ImageGUIElementSize);
            var tooltip = go.AddComponent<BoundTooltipTrigger>();
            tooltip.text = "This is my Tooltip Text";
            var boundTooltip = go.AddComponent<Image>();
            boundTooltip.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);

            // if there is no ToolTipItem add one...
            CreateToolTipItem(false, go);
            Selection.activeGameObject = go;
        }

        private static void CreateToolTipItem(bool select)
        {
            CreateToolTipItem(select, null);
        }

        private static void CreateToolTipItem(bool select, GameObject parent)
        {
#if UNITY_2023_1_OR_NEWER
			var btti = Object.FindFirstObjectByType<BoundTooltipItem>();
#else
            var btti = Object.FindObjectOfType<BoundTooltipItem>();
#endif
            if (btti == null)
            {
                var boundTooltipItem = CreateUIObject("ToolTipItem", parent.GetComponentInParent<Canvas>().gameObject);
                btti = boundTooltipItem.AddComponent<BoundTooltipItem>();
                var boundTooltipItemCanvasGroup = boundTooltipItem.AddComponent<CanvasGroup>();
                boundTooltipItemCanvasGroup.interactable = false;
                boundTooltipItemCanvasGroup.blocksRaycasts = false;
                var boundTooltipItemImage = boundTooltipItem.AddComponent<Image>();
                boundTooltipItemImage.sprite =
                    AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
                var boundTooltipItemText = CreateUIObject("Text", boundTooltipItem);
                var boundTooltipItemTextRT = boundTooltipItemText.GetComponent<RectTransform>();
                boundTooltipItemTextRT.anchorMin = Vector2.zero;
                boundTooltipItemTextRT.anchorMax = Vector2.one;
                boundTooltipItemTextRT.sizeDelta = Vector2.one;
                var boundTooltipItemTextcomponent = boundTooltipItemText.AddComponent<Text>();
                boundTooltipItemTextcomponent.alignment = TextAnchor.MiddleCenter;
                Undo.RegisterCreatedObjectUndo(boundTooltipItem, "Create " + boundTooltipItem.name);
            }

            if (select && btti != null) Selection.activeGameObject = btti.gameObject;
        }

        #endregion

        #region Progress bar

        [MenuItem("GameObject/UI/Extensions/Controls/Progress Bar", false)]
        public static void AddSlider(MenuCommand menuCommand)
        {
            // Create GOs Hierarchy
            var root = CreateUIElementRoot("Progress Bar", menuCommand, s_ThinGUIElementSize);

            var background = CreateUIObject("Background", root);
            var fillArea = CreateUIObject("Fill Area", root);
            var fill = CreateUIObject("Fill", fillArea);

            // Background
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = s_DefaultSelectableColor;
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.sizeDelta = new Vector2(0, 0);

            // Fill Area
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = s_DefaultSelectableColor;

            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;

            // Setup slider component
            var slider = root.AddComponent<Slider>();
            slider.value = 0;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.direction = Slider.Direction.LeftToRight;
            SetDefaultColorTransitionValues(slider);
        }

        #endregion

        #region Primitives

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Line Renderer", false)]
        public static void AddUILineRenderer(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI LineRenderer", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UILineRenderer>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Line Texture Renderer", false)]
        public static void AddUILineTextureRenderer(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI LineTextureRenderer", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UILineTextureRenderer>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Squircle", false)]
        public static void AddUISquircle(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Squircle", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UISquircle>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Circle", false)]
        public static void AddUICircle(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Circle", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UICircle>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Diamond Graph", false)]
        public static void AddDiamondGraph(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Diamond Graph", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<DiamondGraph>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Cut Corners", false)]
        public static void AddCutCorners(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Cut Corners", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UICornerCut>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Polygon", false)]
        public static void AddPolygon(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Polygon", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UIPolygon>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Primitives/UI Grid Renderer", false)]
        public static void AddUIGridRenderer(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI GridRenderer", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<UIGridRenderer>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region Re-Orderable Lists

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Vertical Scroll Rect", false)]
        public static void AddReorderableScrollRectVertical(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot(
                "Re-orderable Vertical ScrollRect",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);


            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var sr = reorderableScrollRoot.AddComponent<ScrollRect>();
            sr.vertical = true;
            sr.horizontal = false;

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentVLG = childContent.AddComponent<VerticalLayoutGroup>();
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = rectTransformContent;
            reorderableScrollRootRL.ContentLayout = childContentVLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minHeight = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minHeight = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minHeight = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Horizontal Scroll Rect", false)]
        public static void AddReorderableScrollRectHorizontal(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot(
                "Re-orderable Horizontal ScrollRect",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);


            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var sr = reorderableScrollRoot.AddComponent<ScrollRect>();
            sr.vertical = false;
            sr.horizontal = true;

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentHLG = childContent.AddComponent<HorizontalLayoutGroup>();
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = rectTransformContent;
            reorderableScrollRootRL.ContentLayout = childContentHLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minWidth = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minWidth = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minWidth = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Grid Scroll Rect", false)]
        public static void AddReorderableScrollRectGrid(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot(
                "Re-orderable Grid ScrollRect",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);


            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var sr = reorderableScrollRoot.AddComponent<ScrollRect>();
            sr.vertical = true;
            sr.horizontal = false;

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentGLG = childContent.AddComponent<GridLayoutGroup>();
            childContentGLG.cellSize = new Vector2(30, 30);
            childContentGLG.spacing = new Vector2(10, 10);
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = rectTransformContent;
            reorderableScrollRootRL.ContentLayout = childContentGLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minHeight = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minHeight = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minHeight = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Vertical List", false)]
        public static void AddReorderableVerticalList(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot(
                "Re-orderable Vertical List",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);

            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentVLG = childContent.AddComponent<VerticalLayoutGroup>();
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            reorderableScrollRootRL.ContentLayout = childContentVLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minHeight = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minHeight = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minHeight = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Horizontal List", false)]
        public static void AddReorderableHorizontalList(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot(
                "Re-orderable Horizontal List",
                menuCommand,
                s_ThickGUIElementSize
            );

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);


            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentHLG = childContent.AddComponent<HorizontalLayoutGroup>();
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            reorderableScrollRootRL.ContentLayout = childContentHLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minWidth = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minWidth = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minWidth = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        [MenuItem("GameObject/UI/Extensions/Re-orderable Lists/Re-orderable Grid", false)]
        public static void AddReorderableGrid(MenuCommand menuCommand)
        {
            var reorderableScrollRoot = CreateUIElementRoot("Re-orderable Grid", menuCommand, s_ThickGUIElementSize);

            var childContent = CreateUIObject("List_Content", reorderableScrollRoot);

            var Element01 = CreateUIObject("Element_01", childContent);
            var Element02 = CreateUIObject("Element_02", childContent);
            var Element03 = CreateUIObject("Element_03", childContent);


            // Set RectTransform to stretch
            var rectTransformScrollSnapRoot = reorderableScrollRoot.GetComponent<RectTransform>();
            rectTransformScrollSnapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformScrollSnapRoot.anchoredPosition = Vector2.zero;
            rectTransformScrollSnapRoot.sizeDelta = new Vector2(150f, 150f);


            var image = reorderableScrollRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.392f);

            var reorderableScrollRootLE = reorderableScrollRoot.AddComponent<LayoutElement>();
            reorderableScrollRootLE.preferredHeight = 300;

            reorderableScrollRoot.AddComponent<Mask>();
            var reorderableScrollRootRL = reorderableScrollRoot.AddComponent<ReorderableList>();

            //Setup Content container
            var rectTransformContent = childContent.GetComponent<RectTransform>();
            rectTransformContent.anchorMin = Vector2.zero;
            rectTransformContent.anchorMax = new Vector2(1f, 1f);
            rectTransformContent.sizeDelta = Vector2.zero;
            var childContentGLG = childContent.AddComponent<GridLayoutGroup>();
            childContentGLG.cellSize = new Vector2(30, 30);
            childContentGLG.spacing = new Vector2(10, 10);
            var childContentCSF = childContent.AddComponent<ContentSizeFitter>();
            childContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            reorderableScrollRootRL.ContentLayout = childContentGLG;

            //Setup 1st Child
            var Element01Image = Element01.AddComponent<Image>();
            Element01Image.color = Color.green;

            var rectTransformElement01 = Element01.GetComponent<RectTransform>();
            rectTransformElement01.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement01.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement01.pivot = new Vector2(0.5f, 0.5f);

            var LEElement01 = Element01.AddComponent<LayoutElement>();
            LEElement01.minHeight = 50;

            //Setup 2nd Child
            var Element02Image = Element02.AddComponent<Image>();
            Element02Image.color = Color.red;

            var rectTransformElement02 = Element02.GetComponent<RectTransform>();
            rectTransformElement02.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement02.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement02.pivot = new Vector2(0.5f, 0.5f);

            var LEElement02 = Element02.AddComponent<LayoutElement>();
            LEElement02.minHeight = 50;

            //Setup 2nd Child
            var Element03Image = Element03.AddComponent<Image>();
            Element03Image.color = Color.blue;

            var rectTransformElement03 = Element03.GetComponent<RectTransform>();
            rectTransformElement03.anchorMin = new Vector2(0f, 0.5f);
            rectTransformElement03.anchorMax = new Vector2(0f, 0.5f);
            rectTransformElement03.pivot = new Vector2(0.5f, 0.5f);

            var LEElement03 = Element03.AddComponent<LayoutElement>();
            LEElement03.minHeight = 50;


            //Need to add example child components like in the Asset (SJ)
            Selection.activeGameObject = reorderableScrollRoot;
        }

        #endregion

        #region Segmented Control

        [MenuItem("GameObject/UI/Extensions/Controls/Segmented Control", false)]
        public static void AddSegmentedControl(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Segmented Control", menuCommand, s_ThickGUIElementSize);
            var control = go.AddComponent<SegmentedControl>();

            var selectedColor = new Color(0f, 0.455f, 0.894f);

            var labels = new[] { "This", "That", "Other" };
            for (var i = 0; i < 3; i++)
            {
                var button = AddButtonAsChild(go).GetComponent<Button>();
                button.gameObject.AddComponent<Segment>();
                button.name = "Segment " + (i + 1);

                var colors = button.colors;
                colors.pressedColor = selectedColor;
                button.colors = colors;

                var text = button.GetComponentInChildren<Text>();
                text.text = labels[i];
                text.color = selectedColor;
            }

            control.LayoutSegments();

            Selection.activeGameObject = go;
        }

        #endregion

        #region Stepper

        [MenuItem("GameObject/UI/Extensions/Sliders/Stepper", false)]
        public static void AddStepper(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("Stepper", menuCommand, new Vector2(kWidth / 2, kThickHeight));
            var control = go.AddComponent<Stepper>();

            var labels = new[] { "−", "+" };
            for (var i = 0; i < 2; i++)
            {
                var button = AddButtonAsChild(go);
                button.gameObject.AddComponent<StepperSide>();
                button.name = i == 0 ? "Minus" : "Plus";
                var text = button.GetComponentInChildren<Text>();
                text.text = labels[i];
            }

            control.LayoutSides();

            Selection.activeGameObject = go;
        }

        #endregion

        #region UI Knob

        [MenuItem("GameObject/UI/Extensions/Controls/UI Knob", false)]
        public static void AddUIKnob(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("UI Knob", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<Image>();
            go.AddComponent<UI_Knob>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region TextPic

        [MenuItem("GameObject/UI/Extensions/Controls/TextPic", false)]
        public static void AddTextPic(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("TextPic", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<TextPic>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region BoxSlider

        [MenuItem("GameObject/UI/Extensions/Sliders/Box Slider", false)]
        public static void AddBoxSlider(MenuCommand menuCommand)
        {
            var uiboxSliderRoot = CreateUIElementRoot("Box Slider", menuCommand, s_ImageGUIElementSize);

            var handleSlideArea = CreateUIObject("Handle Slide Area", uiboxSliderRoot);

            var handle = CreateUIObject("Handle", handleSlideArea);

            // Set RectTransform to stretch
            SetAnchorsAndStretch(uiboxSliderRoot);
            var backgroundImage = uiboxSliderRoot.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.fillCenter = false;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.392f);

            var handleRect = SetAnchorsAndStretch(handle);
            handleRect.sizeDelta = new Vector2(25, 25);
            var handleImage = handle.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
            handleImage.type = Image.Type.Simple;
            handleImage.fillCenter = false;
            handleImage.color = new Color(1f, 1f, 1f, 0.392f);


            var selectableArea = uiboxSliderRoot.AddComponent<BoxSlider>();
            selectableArea.HandleRect = handle.GetComponent<RectTransform>();
            selectableArea.ValueX = selectableArea.ValueY = 0.5f;

            Selection.activeGameObject = uiboxSliderRoot;
        }

        #endregion

        #region Non Drawing  Graphic options

        [MenuItem("GameObject/UI/Extensions/Controls/NonDrawingGraphic", false)]
        public static void AddNonDrawingGraphic(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("NonDrawing Graphic", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<NonDrawingGraphic>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/UI/Extensions/Controls/NonDrawingGraphicClickable", false)]
        public static void AddClickableNonDrawingGraphic(MenuCommand menuCommand)
        {
            var go = CreateUIElementRoot("NonDrawing Graphic-Clickable", menuCommand, s_ImageGUIElementSize);
            go.AddComponent<NonDrawingGraphic>();
            go.AddComponent<UISelectableExtension>();
            Selection.activeGameObject = go;
        }

        #endregion

        #region Radial Slider

        [MenuItem("GameObject/UI/Extensions/Sliders/Radial Slider", false)]
        public static void AddRadialSlider(MenuCommand menuCommand)
        {
            var sliderRoot = CreateUIElementRoot("Radial Slider", menuCommand, s_ThickGUIElementSize);
            var SliderControl = CreateUIObject("Slider", sliderRoot);

            var image = sliderRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            image.type = Image.Type.Simple;
            image.color = s_DefaultSelectableColor;

            var sliderRootRectTransform = sliderRoot.GetComponent<RectTransform>();
            sliderRootRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRootRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRootRectTransform.anchoredPosition = Vector2.zero;
            sliderRootRectTransform.sizeDelta = new Vector2(250f, 250f);

            var slidrImage = SliderControl.AddComponent<Image>();
            slidrImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            slidrImage.type = Image.Type.Filled;
            slidrImage.fillMethod = Image.FillMethod.Radial360;
            slidrImage.fillOrigin = 3;
            slidrImage.color = Color.red;
            slidrImage.fillAmount = 0;
            var slider = SliderControl.AddComponent<RadialSlider>();
            slider.StartColor = Color.green;
            slider.EndColor = Color.red;

            var sliderRectTransform = SliderControl.GetComponent<RectTransform>();
            sliderRectTransform.anchorMin = Vector2.zero;
            sliderRectTransform.anchorMax = Vector2.one;
            sliderRectTransform.sizeDelta = Vector2.zero;

            Selection.activeGameObject = sliderRoot;
        }

        #endregion

        #region RangeSlider

        [MenuItem("GameObject/UI/Extensions/Sliders/Range Slider", false)]
        public static void AddRangeSlider(MenuCommand menuCommand)
        {
            var minMaxSliderRoot = CreateUIElementRoot("Range Slider", menuCommand, new Vector2(160, 20));

            var background = CreateUIObject("Background", minMaxSliderRoot);

            var fillArea = CreateUIObject("Fill Area", minMaxSliderRoot);
            var fill = CreateUIObject("Fill", fillArea);

            var handleSlideArea = CreateUIObject("Handle Slide Area", minMaxSliderRoot);
            var minHandle = CreateUIObject("Low Handle", handleSlideArea);
            var highHandle = CreateUIObject("High Handle", handleSlideArea);

            SetAnchorsAndStretch(minMaxSliderRoot);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.fillCenter = false;

            var backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.sizeDelta = Vector2.zero;

            var fillAreaRect = SetAnchorsAndStretch(fillArea);
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillRect = SetAnchorsAndStretch(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            fillImage.type = Image.Type.Sliced;
            fillImage.fillCenter = true;
            fillRect.offsetMin = new Vector2(-5, 0);
            fillRect.offsetMax = new Vector2(5, 0);

            var handleSlideRect = SetAnchorsAndStretch(handleSlideArea);
            handleSlideRect.anchorMin = new Vector2(0, 0.5f);
            handleSlideRect.anchorMax = new Vector2(1, 0.5f);
            handleSlideRect.offsetMin = new Vector2(10, -10);
            handleSlideRect.offsetMax = new Vector2(-10, 10);

            var lowHandleRect = SetAnchorsAndStretch(minHandle);
            var lowHandleImage = minHandle.AddComponent<Image>();
            lowHandleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
            lowHandleRect.sizeDelta = new Vector2(20, 0);

            var highHandleRect = SetAnchorsAndStretch(highHandle);
            var highHandleImage = highHandle.AddComponent<Image>();
            highHandleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
            highHandleRect.sizeDelta = new Vector2(20, 0);

            var rangeSlider = minMaxSliderRoot.AddComponent<RangeSlider>();
            rangeSlider.FillRect = fillRect;
            rangeSlider.LowHandleRect = lowHandleRect;
            rangeSlider.HighHandleRect = highHandleRect;
            rangeSlider.LowValue = rangeSlider.MinValue;
            rangeSlider.HighValue = rangeSlider.MaxValue;
            rangeSlider.targetGraphic = fillImage;

            Selection.activeGameObject = minMaxSliderRoot;
        }

        #endregion

        #region Menu Manager GO

        [MenuItem("GameObject/UI/Extensions/Menu Manager", false)]
        public static void AddMenuManager(MenuCommand menuCommand)
        {
            var child = new GameObject("MenuManager");
            Undo.RegisterCreatedObjectUndo(child, "Create " + "MenuManager");
            child.AddComponent<MenuManager>();
            Selection.activeGameObject = child;
        }

        #endregion

        #region MinMaxSlider

        [MenuItem("GameObject/UI/Extensions/Sliders/MinMax Slider", false)]
        public static void AddMinMaxSlider(MenuCommand menuCommand)
        {
            var minMaxSliderRoot = CreateUIElementRoot("Min Max Slider", menuCommand, new Vector2(390, 60));
            var sliderBounds = CreateUIObject("Slider Bounds", minMaxSliderRoot);
            var middleGraphic = CreateUIObject("Middle Graphic", minMaxSliderRoot);
            var minHandle = CreateUIObject("Min Handle", minMaxSliderRoot);
            var minHandleText = CreateUIObject("Min Text", minHandle);
            var maxHandle = CreateUIObject("Max Handle", minMaxSliderRoot);
            var maxHandleText = CreateUIObject("Max Text", maxHandle);

            var backgroundImage = minMaxSliderRoot.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.fillCenter = false;
            backgroundImage.color = new Color32(27, 41, 89, 255);
            backgroundImage.fillCenter = true;

            var sliderBoundsRect = SetAnchorsAndStretch(sliderBounds);
            sliderBoundsRect.anchorMin = new Vector2(0, 0);
            sliderBoundsRect.anchorMax = new Vector2(1, 1);
            sliderBoundsRect.offsetMin = new Vector2(15, 0);
            sliderBoundsRect.offsetMax = new Vector2(-15, 0);

            var middleGraphicRect = SetAnchorsAndStretch(middleGraphic);
            middleGraphicRect.anchorMin = new Vector2(0, 0);
            middleGraphicRect.anchorMax = new Vector2(1, 1);
            middleGraphicRect.offsetMin = new Vector2(82, 0);
            middleGraphicRect.offsetMax = new Vector2(-90, 0);
            var fillImage = middleGraphic.AddComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            fillImage.type = Image.Type.Sliced;
            fillImage.fillCenter = true;
            fillImage.color = new Color32(41, 98, 164, 255);

            var minHandleRect = SetAnchorsAndStretch(minHandle);
            minHandleRect.anchorMin = new Vector2(0, 0.5f);
            minHandleRect.anchorMax = new Vector2(0, 0.5f);
            minHandleRect.sizeDelta = new Vector2(30, 62);
            minHandleRect.anchoredPosition = new Vector3(82, 0, 0);
            var minHandleImage = minHandle.AddComponent<Image>();
            minHandleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            minHandleImage.type = Image.Type.Sliced;
            minHandleImage.fillCenter = true;

            var minHandleTextRect = SetAnchorsAndStretch(minHandleText);
            minHandleTextRect.sizeDelta = new Vector2(70, 50);
            minHandleTextRect.anchoredPosition = new Vector3(0, 60, 0);
            var minHandleTextComponent = minHandleText.AddComponent<TextMeshProUGUI>();
            minHandleTextComponent.fontSize = 36;
            minHandleTextComponent.alignment = TextAlignmentOptions.Center;


            var maxHandleRect = SetAnchorsAndStretch(maxHandle);
            maxHandleRect.anchorMin = new Vector2(1, 0.5f);
            maxHandleRect.anchorMax = new Vector2(1, 0.5f);
            maxHandleRect.sizeDelta = new Vector2(30, 62);
            maxHandleRect.anchoredPosition = new Vector3(-82, 0, 0);
            var maxHandleImage = maxHandle.AddComponent<Image>();
            maxHandleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            maxHandleImage.type = Image.Type.Sliced;
            maxHandleImage.fillCenter = true;

            var maxHandleTextRect = SetAnchorsAndStretch(maxHandleText);
            maxHandleTextRect.sizeDelta = new Vector2(70, 50);
            maxHandleTextRect.anchoredPosition = new Vector3(0, 60, 0);
            var maxHandleTextComponent = maxHandleText.AddComponent<TextMeshProUGUI>();
            maxHandleTextComponent.fontSize = 36;
            maxHandleTextComponent.alignment = TextAlignmentOptions.Center;

            var minMaxSlider = minMaxSliderRoot.AddComponent<MinMaxSlider>();
            minMaxSlider.SliderBounds = sliderBoundsRect;
            minMaxSlider.MinHandle = minHandleRect;
            minMaxSlider.MaxHandle = maxHandleRect;
            minMaxSlider.MiddleGraphic = middleGraphicRect;
            minMaxSlider.MinText = minHandleTextComponent;
            minMaxSlider.MaxText = maxHandleTextComponent;

            minMaxSlider.SetValues(minMaxSlider.Values.minValue, minMaxSlider.Values.maxValue);

            Selection.activeGameObject = minMaxSliderRoot;
        }

        #endregion

        #endregion

        #region Helper Functions

        private static GameObject AddInputFieldAsChild(GameObject parent)
        {
            var root = CreateUIObject("InputField", parent);

            var childPlaceholder = CreateUIObject("Placeholder", root);
            var childText = CreateUIObject("Text", root);

            var image = root.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kInputFieldBackgroundPath);
            image.type = Image.Type.Sliced;
            image.color = s_DefaultSelectableColor;

            var inputField = root.AddComponent<InputField>();
            SetDefaultColorTransitionValues(inputField);

            var text = childText.AddComponent<Text>();
            text.text = "";
            text.supportRichText = false;
            SetDefaultTextValues(text);

            var placeholder = childPlaceholder.AddComponent<Text>();
            placeholder.text = "Enter text...";
            placeholder.fontStyle = FontStyle.Italic;
            // Make placeholder color half as opaque as normal text color.
            var placeholderColor = text.color;
            placeholderColor.a *= 0.5f;
            placeholder.color = placeholderColor;

            var textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.offsetMin = new Vector2(10, 6);
            textRectTransform.offsetMax = new Vector2(-10, -7);

            var placeholderRectTransform = childPlaceholder.GetComponent<RectTransform>();
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.sizeDelta = Vector2.zero;
            placeholderRectTransform.offsetMin = new Vector2(10, 6);
            placeholderRectTransform.offsetMax = new Vector2(-10, -7);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            return root;
        }

        private static GameObject AddScrollbarAsChild(GameObject parent)
        {
            // Create GOs Hierarchy
            var scrollbarRoot = CreateUIObject("Scrollbar", parent);

            var sliderArea = CreateUIObject("Sliding Area", scrollbarRoot);
            var handle = CreateUIObject("Handle", sliderArea);

            var bgImage = scrollbarRoot.AddComponent<Image>();
            bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
            bgImage.type = Image.Type.Sliced;
            bgImage.color = s_DefaultSelectableColor;

            var handleImage = handle.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            handleImage.type = Image.Type.Sliced;
            handleImage.color = s_DefaultSelectableColor;

            var sliderAreaRect = sliderArea.GetComponent<RectTransform>();
            sliderAreaRect.sizeDelta = new Vector2(-20, -20);
            sliderAreaRect.anchorMin = Vector2.zero;
            sliderAreaRect.anchorMax = Vector2.one;

            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            var scrollbar = scrollbarRoot.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            SetDefaultColorTransitionValues(scrollbar);

            return scrollbarRoot;
        }

        private static GameObject AddTextAsChild(GameObject parent)
        {
            var go = CreateUIObject("Text", parent);

            var lbl = go.AddComponent<Text>();
            lbl.text = "New Text";
            SetDefaultTextValues(lbl);

            return go;
        }

        private static GameObject AddImageAsChild(GameObject parent)
        {
            var go = CreateUIObject("Image", parent);

            go.AddComponent<Image>();

            return go;
        }

        private static GameObject AddButtonAsChild(GameObject parent)
        {
            var buttonRoot = CreateUIObject("Button", parent);

            var childText = new GameObject("Text");
            GameObjectUtility.SetParentAndAlign(childText, buttonRoot);

            var image = buttonRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            image.type = Image.Type.Sliced;
            image.color = s_DefaultSelectableColor;

            var bt = buttonRoot.AddComponent<Button>();
            SetDefaultColorTransitionValues(bt);

            var text = childText.AddComponent<Text>();
            text.text = "Button";
            text.alignment = TextAnchor.MiddleCenter;
            SetDefaultTextValues(text);

            var textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;

            return buttonRoot;
        }

        private static RectTransform SetAnchorsAndStretch(GameObject root)
        {
            var rectTransformRoot = root.GetComponent<RectTransform>();
            rectTransformRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransformRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransformRoot.anchoredPosition = Vector2.zero;
            return rectTransformRoot;
        }

        #endregion
    }
}