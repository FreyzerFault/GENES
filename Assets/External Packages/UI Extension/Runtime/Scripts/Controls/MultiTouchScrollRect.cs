/// Credit Erdener Gonenc - @PixelEnvision
/*USAGE: Simply use that instead of the regular ScrollRect */

using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/MultiTouchScrollRect")]
    public class MultiTouchScrollRect : ScrollRect
    {
        private int pid = -100;

        /// <summary>
        ///     Begin drag event
        /// </summary>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            pid = eventData.pointerId;
            base.OnBeginDrag(eventData);
        }

        /// <summary>
        ///     Drag event
        /// </summary>
        public override void OnDrag(PointerEventData eventData)
        {
            if (pid == eventData.pointerId) base.OnDrag(eventData);
        }

        /// <summary>
        ///     End drag event
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData)
        {
            pid = -100;
            base.OnEndDrag(eventData);
        }
    }
}