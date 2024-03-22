///Credit perchik
///Sourced from - http://forum.unity3d.com/threads/receive-onclick-event-and-pass-it-on-to-lower-ui-elements.293642/

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Extensions/ComboBox/ComboBox")]
    public class ComboBox : MonoBehaviour
    {
        [Header("Combo Box Items")] public List<string> AvailableOptions;

        [Header("Properties")] [SerializeField]
        private bool isActive = true;

        [SerializeField] private float _scrollBarWidth = 20.0f;

        [SerializeField] private int _itemsToDisplay;

        [SerializeField] private float dropdownOffset;

        [SerializeField] private bool _displayPanelAbove;

        public bool SelectFirstItemOnStart;

        [SerializeField] private int selectItemIndexOnStart;

        [Header("Events")]
        // fires when item is changed;
        public SelectionChangedEvent OnSelectionChanged;

        // fires when item is changed;
        public ControlDisabledEvent OnControlDisabled;
        private Canvas _canvas;
        private RectTransform _canvasRT;
        private bool _hasDrawnOnce;
        private bool _initialized;
        private RectTransform _inputRT;

        //private bool isInitialized = false;
        private bool _isPanelActive;
        private RectTransform _itemsPanelRT;
        private InputField _mainInput;
        private RectTransform _overlayRT;
        private List<string> _panelItems; //items that will get shown in the drop-down
        private RectTransform _rectTransform;
        private RectTransform _scrollBarRT;
        private RectTransform _scrollHandleRT;
        private RectTransform _scrollPanelRT;
        private ScrollRect _scrollRect;
        private RectTransform _slidingAreaRT;
        private GameObject itemTemplate;
        private Dictionary<string, GameObject> panelObjects;
        public DropDownListItem SelectedItem { get; }

        private bool shouldSelectItemOnStart => SelectFirstItemOnStart || selectItemIndexOnStart > 0;

        public string Text { get; private set; }

        public float ScrollBarWidth
        {
            get => _scrollBarWidth;
            set
            {
                _scrollBarWidth = value;
                RedrawPanel();
            }
        }

        public int ItemsToDisplay
        {
            get => _itemsToDisplay;
            set
            {
                _itemsToDisplay = value;
                RedrawPanel();
            }
        }

        public void Awake()
        {
            Initialize();
        }

        public void Start()
        {
            if (shouldSelectItemOnStart && AvailableOptions.Count > 0)
                SelectItemIndex(SelectFirstItemOnStart ? 0 : selectItemIndexOnStart);
            RedrawPanel();
        }

        private bool Initialize()
        {
            if (_initialized) return true;

            var success = true;
            try
            {
                _rectTransform = GetComponent<RectTransform>();
                _inputRT = _rectTransform.Find("InputField").GetComponent<RectTransform>();
                _mainInput = _inputRT.GetComponent<InputField>();

                _overlayRT = _rectTransform.Find("Overlay").GetComponent<RectTransform>();
                _overlayRT.gameObject.SetActive(false);


                _scrollPanelRT = _overlayRT.Find("ScrollPanel").GetComponent<RectTransform>();
                _scrollBarRT = _scrollPanelRT.Find("Scrollbar").GetComponent<RectTransform>();
                _slidingAreaRT = _scrollBarRT.Find("SlidingArea").GetComponent<RectTransform>();
                _scrollHandleRT = _slidingAreaRT.Find("Handle").GetComponent<RectTransform>();
                _itemsPanelRT = _scrollPanelRT.Find("Items").GetComponent<RectTransform>();
                //itemPanelLayout = itemsPanelRT.gameObject.GetComponent<LayoutGroup>();

                _canvas = GetComponentInParent<Canvas>();
                _canvasRT = _canvas.GetComponent<RectTransform>();

                _scrollRect = _scrollPanelRT.GetComponent<ScrollRect>();
                _scrollRect.scrollSensitivity = _rectTransform.sizeDelta.y / 2;
                _scrollRect.movementType = ScrollRect.MovementType.Clamped;
                _scrollRect.content = _itemsPanelRT;

                itemTemplate = _rectTransform.Find("ItemTemplate").gameObject;
                itemTemplate.SetActive(false);
            }
            catch (NullReferenceException ex)
            {
                Debug.LogException(ex);
                Debug.LogError(
                    "Something is setup incorrectly with the dropdownlist component causing a Null Reference Exception"
                );
                success = false;
            }

            panelObjects = new Dictionary<string, GameObject>();

            _panelItems = AvailableOptions.ToList();

            _initialized = true;

            RebuildPanel();
            return success;
        }

        /// <summary>
        ///     Update the drop down selection to a specific index
        /// </summary>
        /// <param name="index"></param>
        public void SelectItemIndex(int index)
        {
            ToggleDropdownPanel(false);
            OnItemClicked(AvailableOptions[index]);
        }

        public void AddItem(string item)
        {
            AvailableOptions.Add(item);
            RebuildPanel();
        }

        public void RemoveItem(string item)
        {
            AvailableOptions.Remove(item);
            RebuildPanel();
        }

        public void SetAvailableOptions(List<string> newOptions)
        {
            var uniqueOptions = newOptions.Distinct().ToArray();
            SetAvailableOptions(uniqueOptions);
        }

        public void SetAvailableOptions(string[] newOptions)
        {
            var uniqueOptions = newOptions.Distinct().ToList();
            if (newOptions.Length != uniqueOptions.Count)
                Debug.LogWarning(
                    $"{nameof(ComboBox)}.{nameof(SetAvailableOptions)}: items may only exists once. {newOptions.Length - uniqueOptions.Count} duplicates."
                );

            AvailableOptions.Clear();

            for (var i = 0; i < newOptions.Length; i++) AvailableOptions.Add(newOptions[i]);

            RebuildPanel();
            RedrawPanel();
        }

        public void ResetItems()
        {
            AvailableOptions.Clear();
            RebuildPanel();
            RedrawPanel();
        }

        /// <summary>
        ///     Rebuilds the contents of the panel in response to items being added.
        /// </summary>
        private void RebuildPanel()
        {
            if (!_initialized) Start();

            //panel starts with all options
            _panelItems.Clear();
            foreach (var option in AvailableOptions) _panelItems.Add(option.ToLower());

            var itemObjs = new List<GameObject>(panelObjects.Values);
            panelObjects.Clear();

            var indx = 0;
            while (itemObjs.Count < AvailableOptions.Count)
            {
                var newItem = Instantiate(itemTemplate);
                newItem.name = "Item " + indx;
                newItem.transform.SetParent(_itemsPanelRT, false);
                itemObjs.Add(newItem);
                indx++;
            }

            for (var i = 0; i < itemObjs.Count; i++)
            {
                itemObjs[i].SetActive(i <= AvailableOptions.Count);
                if (i < AvailableOptions.Count)
                {
                    itemObjs[i].name = "Item " + i + " " + _panelItems[i];
#if UNITY_2022_1_OR_NEWER
                    itemObjs[i].transform.Find("Text").GetComponent<TMP_Text>().text =
                        AvailableOptions[i]; //set the text value
#else
                    itemObjs[i].transform.Find("Text").GetComponent<Text>().text =
 AvailableOptions[i]; //set the text value
#endif
                    var itemBtn = itemObjs[i].GetComponent<Button>();
                    itemBtn.onClick.RemoveAllListeners();
                    var textOfItem =
                        _panelItems[i]; //has to be copied for anonymous function or it gets garbage collected away
                    itemBtn.onClick.AddListener(() => { OnItemClicked(textOfItem); });
                    panelObjects[_panelItems[i]] = itemObjs[i];
                }
            }
        }

        /// <summary>
        ///     what happens when an item in the list is selected
        /// </summary>
        /// <param name="item"></param>
        private void OnItemClicked(string item)
        {
            //Debug.Log("item " + item + " clicked");
            Text = item;
            _mainInput.text = Text;
            ToggleDropdownPanel(true);
        }

        private void RedrawPanel()
        {
            var scrollbarWidth =
                _panelItems.Count > ItemsToDisplay
                    ? _scrollBarWidth
                    : 0f; //hide the scrollbar if there's not enough items
            _scrollBarRT.gameObject.SetActive(_panelItems.Count > ItemsToDisplay);

            var dropdownHeight = _itemsToDisplay > 0
                ? _rectTransform.sizeDelta.y * Mathf.Min(_itemsToDisplay, _panelItems.Count)
                : _rectTransform.sizeDelta.y * _panelItems.Count;
            dropdownHeight += dropdownOffset;

            if (!_hasDrawnOnce || _rectTransform.sizeDelta != _inputRT.sizeDelta)
            {
                _hasDrawnOnce = true;
                _inputRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rectTransform.sizeDelta.x);
                _inputRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rectTransform.sizeDelta.y);

                var itemsRemaining = _panelItems.Count - ItemsToDisplay;
                itemsRemaining = itemsRemaining < 0 ? 0 : itemsRemaining;

                _scrollPanelRT.SetParent(transform, true);
                _scrollPanelRT.anchoredPosition = _displayPanelAbove
                    ? new Vector2(0, dropdownOffset + dropdownHeight)
                    : new Vector2(0, -(dropdownOffset + _rectTransform.sizeDelta.y));

                //make the overlay fill the screen
                _overlayRT.SetParent(_canvas.transform, false);
                _overlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _canvasRT.sizeDelta.x);
                _overlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _canvasRT.sizeDelta.y);

                _overlayRT.SetParent(transform, true);
                _scrollPanelRT.SetParent(_overlayRT, true);
            }

            if (_panelItems.Count < 1) return;

            _scrollPanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dropdownHeight);
            _scrollPanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rectTransform.sizeDelta.x);

            _itemsPanelRT.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _scrollPanelRT.sizeDelta.x - scrollbarWidth - 5
            );
            _itemsPanelRT.anchoredPosition = new Vector2(5, 0);

            _scrollBarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scrollbarWidth);
            _scrollBarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dropdownHeight);
            if (scrollbarWidth == 0)
                _scrollHandleRT.gameObject.SetActive(false);
            else
                _scrollHandleRT.gameObject.SetActive(true);

            _slidingAreaRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            _slidingAreaRT.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                dropdownHeight - _scrollBarRT.sizeDelta.x
            );
        }

        public void OnValueChanged(string currText)
        {
            Text = currText;
            RedrawPanel();

            if (_panelItems.Count == 0)
            {
                _isPanelActive = true; //this makes it get turned off
                ToggleDropdownPanel(false);
            }
            else if (!_isPanelActive)
            {
                ToggleDropdownPanel(false);
            }

            OnSelectionChanged.Invoke(Text);
        }

        /// <summary>
        ///     Toggle the drop down list
        /// </summary>
        /// <param name="directClick"> whether an item was directly clicked on</param>
        public void ToggleDropdownPanel(bool directClick)
        {
            if (!isActive) return;

            _isPanelActive = !_isPanelActive;

            _overlayRT.gameObject.SetActive(_isPanelActive);
            if (_isPanelActive)
            {
                transform.SetAsLastSibling();
            }
            else if (directClick)
            {
                // scrollOffset = Mathf.RoundToInt(itemsPanelRT.anchoredPosition.y / _rectTransform.sizeDelta.y); 
            }
        }

        /// <summary>
        ///     Updates the control and sets its active status, determines whether the dropdown will open ot not
        /// </summary>
        /// <param name="status"></param>
        public void SetActive(bool status)
        {
            if (status != isActive) OnControlDisabled?.Invoke(status);
            isActive = status;
        }

        [Serializable]
        public class SelectionChangedEvent : UnityEvent<string>
        {
        }

        [Serializable]
        public class ControlDisabledEvent : UnityEvent<bool>
        {
        }
    }
}