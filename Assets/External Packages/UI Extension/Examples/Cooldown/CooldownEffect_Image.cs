/// Credit SimonDarksideJ
/// Sourced from my head

namespace UnityEngine.UI.Extensions.Examples
{
    [RequireComponent(typeof(Image))]
    public class CooldownEffect_Image : MonoBehaviour
    {
        public CooldownButton cooldown;
        public Text displayText;

        private string originalText;
        private Image target;

        // Use this for initialization
        private void Start()
        {
            if (cooldown == null) Debug.LogError("Missing Cooldown Button assignment");
            target = GetComponent<Image>();
        }

        // Update is called once per frame
        private void Update()
        {
            target.fillAmount = Mathf.Lerp(0, 1, cooldown.CooldownTimeRemaining / cooldown.CooldownTimeout);
            if (displayText) displayText.text = string.Format("{0}%", cooldown.CooldownPercentComplete);
        }

        private void OnEnable()
        {
            if (displayText) originalText = displayText.text;
        }

        private void OnDisable()
        {
            if (displayText) displayText.text = originalText;
        }
    }
}