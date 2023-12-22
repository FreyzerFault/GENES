using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierPoint))]
[CanEditMultipleObjects]
public class BezierPointEditor : Editor
{
    private SerializedProperty handle1Prop;
    private SerializedProperty handle2Prop;
    private readonly HandleFunction[] handlers = { HandleConnected, HandleBroken, HandleAbsent };

    private SerializedProperty handleTypeProp;

    private BezierPoint point;

    private void OnEnable()
    {
        point = (BezierPoint)target;

        handleTypeProp = serializedObject.FindProperty("handleStyle");
        handle1Prop = serializedObject.FindProperty("_handle1");
        handle2Prop = serializedObject.FindProperty("_handle2");
    }

    private void OnSceneGUI()
    {
        Handles.color = Color.green;
        var fmh_28_66_638388323699096612 = point.transform.rotation; var newPosition = Handles.FreeMoveHandle(point.position,
            HandleUtility.GetHandleSize(point.position) * 0.2f, Vector3.zero, Handles.CubeHandleCap);
        if (point.position != newPosition) point.position = newPosition;

        handlers[(int)point.handleStyle](point);

        Handles.color = Color.yellow;
        Handles.DrawLine(point.position, point.globalHandle1);
        Handles.DrawLine(point.position, point.globalHandle2);

        BezierCurveEditor.DrawOtherPoints(point.curve, point);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var newHandleType =
            (BezierPoint.HandleStyle)EditorGUILayout.EnumPopup("Handle Type",
                (BezierPoint.HandleStyle)handleTypeProp.intValue);

        if (newHandleType != (BezierPoint.HandleStyle)handleTypeProp.intValue)
        {
            handleTypeProp.intValue = (int)newHandleType;

            if ((int)newHandleType == 0)
            {
                if (handle1Prop.vector3Value != Vector3.zero)
                {
                    handle2Prop.vector3Value = -handle1Prop.vector3Value;
                }
                else if (handle2Prop.vector3Value != Vector3.zero)
                {
                    handle1Prop.vector3Value = -handle2Prop.vector3Value;
                }
                else
                {
                    handle1Prop.vector3Value = new Vector3(0.1f, 0, 0);
                    handle2Prop.vector3Value = new Vector3(-0.1f, 0, 0);
                }
            }

            else if ((int)newHandleType == 1)
            {
                if (handle1Prop.vector3Value == Vector3.zero && handle2Prop.vector3Value == Vector3.zero)
                {
                    handle1Prop.vector3Value = new Vector3(0.1f, 0, 0);
                    handle2Prop.vector3Value = new Vector3(-0.1f, 0, 0);
                }
            }

            else if ((int)newHandleType == 2)
            {
                handle1Prop.vector3Value = Vector3.zero;
                handle2Prop.vector3Value = Vector3.zero;
            }
        }

        if (handleTypeProp.intValue != 2)
        {
            var newHandle1 = EditorGUILayout.Vector3Field("Handle 1", handle1Prop.vector3Value);
            var newHandle2 = EditorGUILayout.Vector3Field("Handle 2", handle2Prop.vector3Value);

            if (handleTypeProp.intValue == 0)
            {
                if (newHandle1 != handle1Prop.vector3Value)
                {
                    handle1Prop.vector3Value = newHandle1;
                    handle2Prop.vector3Value = -newHandle1;
                }

                else if (newHandle2 != handle2Prop.vector3Value)
                {
                    handle1Prop.vector3Value = -newHandle2;
                    handle2Prop.vector3Value = newHandle2;
                }
            }

            else
            {
                handle1Prop.vector3Value = newHandle1;
                handle2Prop.vector3Value = newHandle2;
            }
        }

        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    private static void HandleConnected(BezierPoint p)
    {
        Handles.color = Color.cyan;

        var fmh_124_66_638388323699125598 = p.transform.rotation; var newGlobal1 = Handles.FreeMoveHandle(p.globalHandle1,
            HandleUtility.GetHandleSize(p.globalHandle1) * 0.15f, Vector3.zero, Handles.SphereHandleCap);

        if (newGlobal1 != p.globalHandle1)
        {
            Undo.RegisterUndo(p, "Move Handle");
            p.globalHandle1 = newGlobal1;
            p.globalHandle2 = -(newGlobal1 - p.position) + p.position;
        }

        var fmh_134_66_638388323699129350 = p.transform.rotation; var newGlobal2 = Handles.FreeMoveHandle(p.globalHandle2,
            HandleUtility.GetHandleSize(p.globalHandle2) * 0.15f, Vector3.zero, Handles.SphereHandleCap);

        if (newGlobal2 != p.globalHandle2)
        {
            Undo.RegisterUndo(p, "Move Handle");
            p.globalHandle1 = -(newGlobal2 - p.position) + p.position;
            p.globalHandle2 = newGlobal2;
        }
    }

    private static void HandleBroken(BezierPoint p)
    {
        Handles.color = Color.cyan;

        var fmh_149_66_638388323699133631 = Quaternion.identity; var newGlobal1 = Handles.FreeMoveHandle(p.globalHandle1,
            HandleUtility.GetHandleSize(p.globalHandle1) * 0.15f, Vector3.zero, Handles.SphereHandleCap);
        var fmh_151_66_638388323699136951 = Quaternion.identity; var newGlobal2 = Handles.FreeMoveHandle(p.globalHandle2,
            HandleUtility.GetHandleSize(p.globalHandle2) * 0.15f, Vector3.zero, Handles.SphereHandleCap);

        if (newGlobal1 != p.globalHandle1)
        {
            Undo.RegisterUndo(p, "Move Handle");
            p.globalHandle1 = newGlobal1;
        }

        if (newGlobal2 != p.globalHandle2)
        {
            Undo.RegisterUndo(p, "Move Handle");
            p.globalHandle2 = newGlobal2;
        }
    }

    private static void HandleAbsent(BezierPoint p)
    {
        p.handle1 = Vector3.zero;
        p.handle2 = Vector3.zero;
    }

    private delegate void HandleFunction(BezierPoint p);
}