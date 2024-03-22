///Credit perchik
///Sourced from - http://forum.unity3d.com/threads/receive-onclick-event-and-pass-it-on-to-lower-ui-elements.293642/

using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions
{
    [Serializable]
    public class DropDownListItem
    {
        [SerializeField] private string _caption;

        [SerializeField] private Sprite _image;

        [SerializeField] private bool _isDisabled;

        [SerializeField] private string _id;

        public UnityAction OnSelect; //action to be called when this item is selected

        internal UnityAction OnUpdate = null; //action to be called when something changes.  

        /// <summary>
        ///     Constructor for Drop Down List panelItems
        /// </summary>
        /// <param name="caption">Caption for the item </param>
        /// <param name="val">ID of the item </param>
        /// <param name="image"></param>
        /// <param name="disabled">Should the item start disabled</param>
        /// <param name="onSelect">UnityAction to be called when this item is selected</param>
        public DropDownListItem(
            string caption = "", string inId = "", Sprite image = null, bool disabled = false,
            UnityAction onSelect = null
        )
        {
            _caption = caption;
            _image = image;
            _id = inId;
            _isDisabled = disabled;
            OnSelect = onSelect;
        }

        /// <summary>
        ///     Caption of the Item
        /// </summary>
        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                if (OnUpdate != null) OnUpdate();
            }
        }

        /// <summary>
        ///     Image component of the Item
        /// </summary>
        public Sprite Image
        {
            get => _image;
            set
            {
                _image = value;
                if (OnUpdate != null) OnUpdate();
            }
        }

        /// <summary>
        ///     Is the Item currently enabled?
        /// </summary>
        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                if (OnUpdate != null) OnUpdate();
            }
        }

        /// <summary>
        ///     ID exists so that an item can have a caption and a value like in traditional windows forms. IE. an item may be a
        ///     student's name, and the ID can be the student's ID number
        /// </summary>
        public string ID
        {
            get => _id;
            set => _id = value;
        }
    }
}