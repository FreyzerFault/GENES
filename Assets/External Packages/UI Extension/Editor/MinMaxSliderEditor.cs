///Credit brogan89
///Sourced from - https://github.com/brogan89/MinMaxSlider

using System;
using UnityEditor;
using UnityEditor.UI;

namespace UnityEngine.UI.Extensions
{
    [CustomEditor(typeof(MinMaxSlider), true)]
    [CanEditMultipleObjects]
    public class MinMaxSliderEditor : SelectableEditor
    {
        private readonly GUIContent label = new("Min Max Values");
        private SerializedProperty _customCamera;
        private SerializedProperty _maxHandle;
        private SerializedProperty _maxLimit;
        private SerializedProperty _maxText;
        private SerializedProperty _maxValue;
        private SerializedProperty _middleGraphic;
        private SerializedProperty _minHandle;
        private SerializedProperty _minLimit;
        private SerializedProperty _minText;
        private SerializedProperty _minValue;

        private SerializedProperty _onValueChanged;
        private SerializedProperty _sliderBounds;
        private SerializedProperty _textFormat;
        private SerializedProperty _wholeNumbers;

        protected override void OnEnable()
        {
            base.OnEnable();
            _customCamera = serializedObject.FindProperty("customCamera");
            _sliderBounds = serializedObject.FindProperty("sliderBounds");
            _minHandle = serializedObject.FindProperty("minHandle");
            _maxHandle = serializedObject.FindProperty("maxHandle");
            _minText = serializedObject.FindProperty("minText");
            _maxText = serializedObject.FindProperty("maxText");
            _textFormat = serializedObject.FindProperty("textFormat");
            _middleGraphic = serializedObject.FindProperty("middleGraphic");
            _minLimit = serializedObject.FindProperty("minLimit");
            _maxLimit = serializedObject.FindProperty("maxLimit");
            _wholeNumbers = serializedObject.FindProperty("wholeNumbers");
            _minValue = serializedObject.FindProperty("minValue");
            _maxValue = serializedObject.FindProperty("maxValue");
            _onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var minLimitOld = _minLimit.floatValue;
            var maxLimitOld = _maxLimit.floatValue;
            var minValueOld = _minValue.floatValue;
            var maxValueOld = _maxValue.floatValue;

            EditorGUILayout.PropertyField(_customCamera);
            EditorGUILayout.PropertyField(_sliderBounds);
            EditorGUILayout.PropertyField(_minHandle);
            EditorGUILayout.PropertyField(_maxHandle);
            EditorGUILayout.PropertyField(_middleGraphic);

            EditorGUILayout.PropertyField(_minText);
            EditorGUILayout.PropertyField(_maxText);
            EditorGUILayout.PropertyField(_textFormat);

            EditorGUILayout.PropertyField(_minLimit);
            EditorGUILayout.PropertyField(_maxLimit);

            EditorGUILayout.PropertyField(_wholeNumbers);
            EditorGUILayout.PropertyField(_minValue);
            EditorGUILayout.PropertyField(_maxValue);

            var minValue = Mathf.Clamp(_minValue.floatValue, _minLimit.floatValue, _maxLimit.floatValue);
            var maxValue = Mathf.Clamp(_maxValue.floatValue, _minLimit.floatValue, _maxLimit.floatValue);
            EditorGUILayout.MinMaxSlider(label, ref minValue, ref maxValue, _minLimit.floatValue, _maxLimit.floatValue);

            var anyValueChanged = !IsEqualFloat(minValueOld, minValue)
                                  || !IsEqualFloat(maxValueOld, maxValue)
                                  || !IsEqualFloat(minLimitOld, _minLimit.floatValue)
                                  || !IsEqualFloat(maxLimitOld, _maxLimit.floatValue);

            if (anyValueChanged)
            {
                var slider = (MinMaxSlider)target;

                // force limits to ints if whole numbers.
                // needed to do this here because it wouldn't set in component script for some reason
                if (slider.wholeNumbers)
                {
                    _minLimit.floatValue = Mathf.RoundToInt(_minLimit.floatValue);
                    _maxLimit.floatValue = Mathf.RoundToInt(_maxLimit.floatValue);
                }

                // set slider values
                slider.SetValues(minValue, maxValue, _minLimit.floatValue, _maxLimit.floatValue);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onValueChanged);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     Returns true if floating point numbers are within 0.01f (close enough to be considered equal)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool IsEqualFloat(float a, float b) => Math.Abs(a - b) < 0.01f;
    }
}