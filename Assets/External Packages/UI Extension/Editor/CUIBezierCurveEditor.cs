/// Credit Titinious (https://github.com/Titinious)
/// Sourced from - https://github.com/Titinious/CurlyUI

using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    [CustomEditor(typeof(CUIBezierCurve))]
    [CanEditMultipleObjects]
    public class CUIBezierCurveEditor : Editor
    {
        protected void OnSceneGUI()
        {
            var script = (CUIBezierCurve)target;

            if (script.ControlPoints != null)
            {
                var controlPoints = script.ControlPoints;

                var handleTransform = script.transform;
                var handleRotation = script.transform.rotation;

                for (var p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
                {
                    EditorGUI.BeginChangeCheck();
                    var newPt = Handles.DoPositionHandle(
                        handleTransform.TransformPoint(controlPoints[p]),
                        handleRotation
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(script, "Move Point");
                        EditorUtility.SetDirty(script);
                        controlPoints[p] = handleTransform.InverseTransformPoint(newPt);
                        script.Refresh();
                    }
                }

                Handles.color = Color.gray;
                Handles.DrawLine(
                    handleTransform.TransformPoint(controlPoints[0]),
                    handleTransform.TransformPoint(controlPoints[1])
                );
                Handles.DrawLine(
                    handleTransform.TransformPoint(controlPoints[1]),
                    handleTransform.TransformPoint(controlPoints[2])
                );
                Handles.DrawLine(
                    handleTransform.TransformPoint(controlPoints[2]),
                    handleTransform.TransformPoint(controlPoints[3])
                );

                var sampleSize = 10;

                Handles.color = Color.white;
                for (var s = 0; s < sampleSize; s++)
                    Handles.DrawLine(
                        handleTransform.TransformPoint(script.GetPoint((float)s / sampleSize)),
                        handleTransform.TransformPoint(script.GetPoint((float)(s + 1) / sampleSize))
                    );

                script.EDITOR_ControlPoints = controlPoints;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}