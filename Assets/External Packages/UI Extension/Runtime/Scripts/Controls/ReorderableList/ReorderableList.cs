/// Credit Ziboo
/// Sourced from - http://forum.unity3d.com/threads/free-reorderable-list.364600/

using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Extensions/Re-orderable list")]
    public class ReorderableList : MonoBehaviour
    {
        [Tooltip("Child container with re-orderable items in a layout group")]
        public LayoutGroup ContentLayout;

        [Tooltip("Parent area to draw the dragged element on top of containers. Defaults to the root Canvas")]
        public RectTransform DraggableArea;

        [Tooltip("Can items be dragged from the container?")]
        public bool IsDraggable = true;

        [Tooltip("Should the draggable components be removed or cloned?")]
        public bool CloneDraggedObject;

        [Tooltip("Can new draggable items be dropped in to the container?")]
        public bool IsDropable = true;

        [Tooltip(
            "Should dropped items displace a current item if the list is full?\n " +
            "Depending on the dropped items origin list, the displaced item may be added, dropped in space or deleted."
        )]
        public bool IsDisplacable;

        // This sets every item size (when being dragged over this list) to the current size of the first element of this list
        [Tooltip("Should items being dragged over this list have their sizes equalized?")]
        public bool EqualizeSizesOnDrag;

        [Tooltip("Maximum number of items this container can hold")]
        public int maxItems = int.MaxValue;

        [Header("UI Re-orderable Events")] public ReorderableListHandler OnElementDropped = new();

        public ReorderableListHandler OnElementGrabbed = new();
        public ReorderableListHandler OnElementRemoved = new();
        public ReorderableListHandler OnElementAdded = new();
        public ReorderableListHandler OnElementDisplacedFrom = new();
        public ReorderableListHandler OnElementDisplacedTo = new();
        public ReorderableListHandler OnElementDisplacedFromReturned = new();
        public ReorderableListHandler OnElementDisplacedToReturned = new();
        public ReorderableListHandler OnElementDroppedWithMaxItems = new();

        private RectTransform _content;
        private ReorderableListContent _listContent;

        public RectTransform Content
        {
            get
            {
                if (_content == null) _content = ContentLayout.GetComponent<RectTransform>();
                return _content;
            }
        }

        private void Start()
        {
            if (ContentLayout == null)
            {
                Debug.LogError("You need to have a child LayoutGroup content set for the list: " + name, gameObject);
                return;
            }

            if (DraggableArea == null)
                DraggableArea = transform.root.GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            if (IsDropable && !GetComponent<Graphic>())
            {
                Debug.LogError(
                    "You need to have a Graphic control (such as an Image) for the list [" + name + "] to be droppable",
                    gameObject
                );
                return;
            }

            Refresh();
        }

        public Canvas GetCanvas()
        {
            var t = transform;
            Canvas canvas = null;


            var lvlLimit = 100;
            var lvl = 0;

            while (canvas == null && lvl < lvlLimit)
            {
                if (!t.gameObject.TryGetComponent(out canvas)) t = t.parent;

                lvl++;
            }

            return canvas;
        }

        /// <summary>
        ///     Refresh related list content
        /// </summary>
        public void Refresh()
        {
            _listContent = ContentLayout.gameObject.GetOrAddComponent<ReorderableListContent>();
            _listContent.Init(this);
        }

        #region Nested type: ReorderableListEventStruct

        [Serializable]
        public struct ReorderableListEventStruct
        {
            public GameObject DroppedObject;
            public int FromIndex;
            public ReorderableList FromList;
            public bool IsAClone;
            public GameObject SourceObject;
            public int ToIndex;
            public ReorderableList ToList;

            public void Cancel()
            {
                SourceObject.GetComponent<ReorderableListElement>().isValid = false;
            }
        }

        #endregion

        #region Nested type: ReorderableListHandler

        [Serializable]
        public class ReorderableListHandler : UnityEvent<ReorderableListEventStruct>
        {
        }

        public void TestReOrderableListTarget(ReorderableListEventStruct item)
        {
            Debug.Log("Event Received");
            Debug.Log("Hello World, is my item a clone? [" + item.IsAClone + "]");
        }

        #endregion
    }
}