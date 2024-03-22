/// Credit setchi (https://github.com/setchi)
/// Sourced from - https://github.com/setchi/FancyScrollView

using UnityEditor;
using UnityEditor.AnimatedValues;

namespace UnityEngine.UI.Extensions
{
    [CustomEditor(typeof(Scroller))]
    [CanEditMultipleObjects]
    public class ScrollerEditor : Editor
    {
        private SerializedProperty decelerationRate;
        private SerializedProperty draggable;
        private SerializedProperty elasticity;
        private SerializedProperty inertia;
        private SerializedProperty movementType;
        private SerializedProperty scrollbar;
        private SerializedProperty scrollDirection;
        private SerializedProperty scrollSensitivity;

        private AnimBool showElasticity;
        private AnimBool showInertiaRelatedValues;
        private SerializedProperty snap;
        private SerializedProperty viewport;

        private void OnEnable()
        {
            viewport = serializedObject.FindProperty("viewport");
            scrollDirection = serializedObject.FindProperty("scrollDirection");
            movementType = serializedObject.FindProperty("movementType");
            elasticity = serializedObject.FindProperty("elasticity");
            scrollSensitivity = serializedObject.FindProperty("scrollSensitivity");
            inertia = serializedObject.FindProperty("inertia");
            decelerationRate = serializedObject.FindProperty("decelerationRate");
            snap = serializedObject.FindProperty("snap");
            draggable = serializedObject.FindProperty("draggable");
            scrollbar = serializedObject.FindProperty("scrollbar");

            showElasticity = new AnimBool(Repaint);
            showInertiaRelatedValues = new AnimBool(Repaint);
            SetAnimBools(true);
        }

        private void OnDisable()
        {
            showElasticity.valueChanged.RemoveListener(Repaint);
            showInertiaRelatedValues.valueChanged.RemoveListener(Repaint);
        }

        private void SetAnimBools(bool instant)
        {
            SetAnimBool(
                showElasticity,
                !movementType.hasMultipleDifferentValues && movementType.enumValueIndex == (int)MovementType.Elastic,
                instant
            );
            SetAnimBool(showInertiaRelatedValues, !inertia.hasMultipleDifferentValues && inertia.boolValue, instant);
        }

        private void SetAnimBool(AnimBool a, bool value, bool instant)
        {
            if (instant)
                a.value = value;
            else
                a.target = value;
        }

        public override void OnInspectorGUI()
        {
            SetAnimBools(false);

            serializedObject.Update();
            EditorGUILayout.PropertyField(viewport);
            EditorGUILayout.PropertyField(scrollDirection);
            EditorGUILayout.PropertyField(movementType);
            DrawMovementTypeRelatedValue();
            EditorGUILayout.PropertyField(scrollSensitivity);
            EditorGUILayout.PropertyField(inertia);
            DrawInertiaRelatedValues();
            EditorGUILayout.PropertyField(draggable);
            EditorGUILayout.PropertyField(scrollbar);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMovementTypeRelatedValue()
        {
            using (var group = new EditorGUILayout.FadeGroupScope(showElasticity.faded))
            {
                if (!group.visible) return;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(elasticity);
                }
            }
        }

        private void DrawInertiaRelatedValues()
        {
            using (var group = new EditorGUILayout.FadeGroupScope(showInertiaRelatedValues.faded))
            {
                if (!group.visible) return;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(decelerationRate);
                    EditorGUILayout.PropertyField(snap);
                }
            }
        }
    }
}