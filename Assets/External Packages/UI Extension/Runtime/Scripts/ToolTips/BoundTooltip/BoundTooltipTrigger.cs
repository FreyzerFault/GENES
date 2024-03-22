///Credit Martin Nerurkar // www.martin.nerurkar.de // www.sharkbombs.com
///Sourced from - http://www.sharkbombs.com/2015/02/10/tooltips-with-the-new-unity-ui-ugui/

using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Bound Tooltip/Bound Tooltip Trigger")]
    public class BoundTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler,
        IDeselectHandler
    {
        [TextAreaAttribute] public string text;

        public bool useMousePosition;

        public Vector3 offset;

        public void OnDeselect(BaseEventData eventData)
        {
            StopHover();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (useMousePosition)
                StartHover(new Vector3(eventData.position.x, eventData.position.y, 0f));
            else
                StartHover(transform.position + offset);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHover();
        }

        public void OnSelect(BaseEventData eventData)
        {
            StartHover(transform.position);
        }

        private void StartHover(Vector3 position)
        {
            BoundTooltipItem.Instance.ShowTooltip(text, position);
        }

        private void StopHover()
        {
            BoundTooltipItem.Instance.HideTooltip();
        }
    }
}