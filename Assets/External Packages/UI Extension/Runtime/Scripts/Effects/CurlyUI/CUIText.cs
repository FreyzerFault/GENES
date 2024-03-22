/// Credit Titinious (https://github.com/Titinious)
/// Sourced from - https://github.com/Titinious/CurlyUI

using TMPro;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform))]
#if UNITY_2022_1_OR_NEWER
    [RequireComponent(typeof(TMP_Text))]
#else
    [RequireComponent(typeof(Text))]
#endif
    [AddComponentMenu("UI/Effects/Extensions/Curly UI Text")]
    public class CUIText : CUIGraphic
    {
        public override void ReportSet()
        {
            if (uiGraphic == null)
            {
#if UNITY_2022_1_OR_NEWER
                uiGraphic = GetComponent<TMP_Text>();
#else
                uiGraphic = GetComponent<Text>();
#endif
            }

            base.ReportSet();
        }
    }
}