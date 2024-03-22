///Credit Martin Nerurkar // www.martin.nerurkar.de // www.sharkbombs.com
///Sourced from - http://www.sharkbombs.com/2015/02/10/tooltips-with-the-new-unity-ui-ugui/

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Bound Tooltip/Bound Tooltip Item")]
    public class BoundTooltipItem : MonoBehaviour
    {
        // Standard Singleton Access
        private static BoundTooltipItem instance;

        public Text TooltipText;
        public Vector3 ToolTipOffset;

        public bool IsActive => gameObject.activeSelf;

        public static BoundTooltipItem Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    instance = GameObject.FindFirstObjectByType<BoundTooltipItem>();
#else
                    instance = FindObjectOfType<BoundTooltipItem>();
#endif
                }

                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
            if (!TooltipText) TooltipText = GetComponentInChildren<Text>();
            HideTooltip();
        }

        public void ShowTooltip(string text, Vector3 pos)
        {
            if (TooltipText.text != text) TooltipText.text = text;

            transform.position = pos + ToolTipOffset;

            gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            gameObject.SetActive(false);
        }
    }
}