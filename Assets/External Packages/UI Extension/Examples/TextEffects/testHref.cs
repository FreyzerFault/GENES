/// Credit playemgames 
/// Sourced from - http://forum.unity3d.com/threads/sprite-icons-with-text-e-g-emoticons.265927/

namespace UnityEngine.UI.Extensions.Examples
{
    public class testHref : MonoBehaviour
    {
        public TextPic textPic;

        private void Awake()
        {
            textPic = GetComponent<TextPic>();
        }

        private void OnEnable()
        {
            textPic.onHrefClick.AddListener(OnHrefClick);
        }

        private void OnDisable()
        {
            textPic.onHrefClick.RemoveListener(OnHrefClick);
        }

        private void OnHrefClick(string hrefName)
        {
            Debug.Log("Click on the " + hrefName);
        }
    }
}