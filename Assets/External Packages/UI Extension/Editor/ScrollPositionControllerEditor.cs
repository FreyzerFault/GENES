/// Credit setchi (https://github.com/setchi)
/// Sourced from - https://github.com/setchi/FancyScrollView

// For maintenance, every new [SerializeField] variable in ScrollPositionController must be declared here

using System;
using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    [Obsolete("ScrollPositionController has been replaced by the Scroller component", true)]
    [CustomEditor(typeof(ScrollPositionController))]
    [CanEditMultipleObjects]
    public class ScrollPositionControllerEditor : Editor
    {
        private SerializedProperty dataCount;
        private SerializedProperty decelerationRate;
        private SerializedProperty directionOfRecognize;
        private SerializedProperty elasticity;
        private SerializedProperty inertia;
        private SerializedProperty movementType;
        private SerializedProperty scrollSensitivity;
        private SerializedProperty snap;
        private SerializedProperty snapDuration;
        private SerializedProperty snapEnable;
        private SerializedProperty snapVelocityThreshold;
        private SerializedProperty viewport;

        private void OnEnable()
        {
            viewport = serializedObject.FindProperty("viewport");
            directionOfRecognize = serializedObject.FindProperty("directionOfRecognize");
            movementType = serializedObject.FindProperty("movementType");
            elasticity = serializedObject.FindProperty("elasticity");
            scrollSensitivity = serializedObject.FindProperty("scrollSensitivity");
            inertia = serializedObject.FindProperty("inertia");
            decelerationRate = serializedObject.FindProperty("decelerationRate");
            snap = serializedObject.FindProperty("snap");
            snapEnable = serializedObject.FindProperty("snap.Enable");
            snapVelocityThreshold = serializedObject.FindProperty("snap.VelocityThreshold");
            snapDuration = serializedObject.FindProperty("snap.Duration");
            dataCount = serializedObject.FindProperty("dataCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(viewport);
            EditorGUILayout.PropertyField(directionOfRecognize);
            EditorGUILayout.PropertyField(movementType);
            EditorGUILayout.PropertyField(elasticity);
            EditorGUILayout.PropertyField(scrollSensitivity);
            EditorGUILayout.PropertyField(inertia);
            DrawInertiaRelatedValues();
            EditorGUILayout.PropertyField(dataCount);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInertiaRelatedValues()
        {
            if (inertia.boolValue)
            {
                EditorGUILayout.PropertyField(decelerationRate);
                EditorGUILayout.PropertyField(snap);

                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSnapRelatedValues();
                }
            }
        }

        private void DrawSnapRelatedValues()
        {
            if (snap.isExpanded)
            {
                EditorGUILayout.PropertyField(snapEnable);

                if (snapEnable.boolValue)
                {
                    EditorGUILayout.PropertyField(snapVelocityThreshold);
                    EditorGUILayout.PropertyField(snapDuration);
                }
            }
        }
    }
}