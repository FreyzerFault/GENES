﻿/// Credit Vicente Russo  
/// Sourced from - https://bitbucket.org/SimonDarksideJ/unity-ui-extensions/issues/23/returnkeytriggersbutton

using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    ///     Usage: Add this component to the input and add the function to execute to the EnterSubmit event of this script
    /// </summary>
    [RequireComponent(typeof(InputField))]
    [AddComponentMenu("UI/Extensions/Input Field Submit")]
    public class InputFieldEnterSubmit : MonoBehaviour
    {
        public EnterSubmitEvent EnterSubmit;
        public bool defocusInput = true;
        private InputField _input;

        private void Awake()
        {
            _input = GetComponent<InputField>();
            _input.onEndEdit.AddListener(OnEndEdit);
        }

        public void OnEndEdit(string txt)
        {
            if (!UIExtensionsInputManager.GetKeyDown(KeyCode.Return)
                && !UIExtensionsInputManager.GetKeyDown(KeyCode.KeypadEnter))
                return;
            EnterSubmit.Invoke(txt);
            if (defocusInput) EventSystem.current.SetSelectedGameObject(null);
        }

        [Serializable]
        public class EnterSubmitEvent : UnityEvent<string>
        {
        }
    }
}