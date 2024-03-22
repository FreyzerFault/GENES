/// Credit Melang 
/// Sourced from - http://forum.unity3d.com/members/melang.593409/
/// Updated SimonDarksideJ - reworked to 4.6.1 standards

using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(InputField))]
    [AddComponentMenu("UI/Extensions/Return Key Trigger")]
    public class ReturnKeyTriggersButton : MonoBehaviour, ISubmitHandler
    {
        public Button button;
        public float highlightDuration = 0.2f;
        private readonly bool highlight = true;
        private EventSystem _system;

        private void Start()
        {
            _system = EventSystem.current;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (highlight) button.OnPointerEnter(new PointerEventData(_system));
            button.OnPointerClick(new PointerEventData(_system));

            if (highlight) Invoke("RemoveHighlight", highlightDuration);
        }

        private void RemoveHighlight()
        {
            button.OnPointerExit(new PointerEventData(_system));
        }
    }
}