/// Credit dakka
/// Sourced from - http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/#post-1752415
/// Notes - Mod from Yilmaz Kiymaz's editor scripts presentation at Unite 2013
/// Updated simonDarksideJ - removed Linq use, not required.

using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    public class CanvasGroupActivator : EditorWindow
    {
        private CanvasGroup[] canvasGroups;

        private void OnEnable()
        {
            ObtainCanvasGroups();
        }

        private void OnGUI()
        {
            if (canvasGroups == null) return;

            GUILayout.Space(10f);
            GUILayout.Label("Canvas Groups");

            for (var i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] == null) continue;

                var initialActive = false;
                if (canvasGroups[i].alpha == 1.0f) initialActive = true;

                var active = EditorGUILayout.Toggle(canvasGroups[i].name, initialActive);
                if (active != initialActive)
                {
                    //If deactivated and initially active
                    if (!active && initialActive)
                    {
                        //Deactivate this
                        canvasGroups[i].alpha = 0f;
                        canvasGroups[i].interactable = false;
                        canvasGroups[i].blocksRaycasts = false;
                    }
                    //If activated and initially deactivate
                    else if (active && !initialActive)
                    {
                        //Deactivate all others and activate this
                        HideAllGroups();

                        canvasGroups[i].alpha = 1.0f;
                        canvasGroups[i].interactable = true;
                        canvasGroups[i].blocksRaycasts = true;
                    }
                }
            }

            GUILayout.Space(5f);

            if (GUILayout.Button("Show All")) ShowAllGroups();

            if (GUILayout.Button("Hide All")) HideAllGroups();
        }

        private void OnFocus()
        {
            ObtainCanvasGroups();
        }

        [MenuItem("Window/UI/Extensions/Canvas Groups Activator")]
        public static void InitWindow()
        {
            GetWindow<CanvasGroupActivator>();
        }

        private void ObtainCanvasGroups()
        {
#if UNITY_2023_1_OR_NEWER
			canvasGroups = GameObject.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
#else
            canvasGroups = FindObjectsOfType<CanvasGroup>();
#endif
        }

        private void ShowAllGroups()
        {
            foreach (var group in canvasGroups)
                if (group != null)
                {
                    group.alpha = 1.0f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
        }

        private void HideAllGroups()
        {
            foreach (var group in canvasGroups)
                if (group != null)
                {
                    group.alpha = 0;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
        }
    }
}