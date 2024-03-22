/// Credit FireOApache 
/// sourced from: http://answers.unity3d.com/questions/1149417/ui-button-onclick-sensitivity-for-high-dpi-devices.html#answer-1197307

/*USAGE:
Simply place the script on A Text control in the scene to display the current PPI / DPI of the screen*/

using TMPro;

namespace UnityEngine.UI.Extensions
{
#if UNITY_2022_1_OR_NEWER
    [RequireComponent(typeof(TMP_Text))]
#else
    [RequireComponent(typeof(Text))]
#endif
    [AddComponentMenu("UI/Extensions/PPIViewer")]
    public class PPIViewer : MonoBehaviour
    {
#if UNITY_2022_1_OR_NEWER
        private TMP_Text label;
#else
        private Text label;
#endif

        private void Awake()
        {
#if UNITY_2022_1_OR_NEWER
            label = GetComponentInChildren<TMP_Text>();
#else
            label = GetComponentInChildren<Text>();
#endif
        }

        private void Start()
        {
            if (label != null) label.text = "PPI: " + Screen.dpi;
        }
    }
}