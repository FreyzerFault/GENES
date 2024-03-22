using System.Collections;
using UnityEngine.EventSystems;

///Credit Martin Nerurkar // www.martin.nerurkar.de // www.sharkbombs.com
///Sourced from - http://www.sharkbombs.com/2015/02/10/tooltips-with-the-new-unity-ui-ugui/

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Extensions/Tooltip/Tooltip Trigger")]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler,
        IDeselectHandler
    {
        public enum TooltipPositioningType
        {
            mousePosition,
            mousePositionAndFollow,
            transformPosition
        }

        [TextAreaAttribute] public string text;

        [Tooltip(
            "Defines where the tooltip will be placed and how that placement will occur. Transform position will always be used if this element wasn't selected via mouse"
        )]
        public TooltipPositioningType tooltipPositioningType = TooltipPositioningType.mousePosition;

        public Vector3 offset;

        private bool hovered;

        /// <summary>
        ///     This info is needed to make sure we make the necessary translations if the tooltip and this trigger are children of
        ///     different space canvases
        /// </summary>
        private bool isChildOfOverlayCanvas;

        /// <summary>
        ///     Checks if the tooltip and the transform this trigger is attached to are children of differently-spaced Canvases
        /// </summary>
        public bool WorldToScreenIsRequired =>
            (isChildOfOverlayCanvas && ToolTip.Instance.guiMode == RenderMode.ScreenSpaceCamera) ||
            (!isChildOfOverlayCanvas && ToolTip.Instance.guiMode == RenderMode.ScreenSpaceOverlay);


        private void Start()
        {
            //attempt to check if our canvas is overlay or not and check our "is overlay" accordingly
            var ourCanvas = GetComponentInParent<Canvas>();
            if (ourCanvas && ourCanvas.renderMode == RenderMode.ScreenSpaceOverlay) isChildOfOverlayCanvas = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            StopHover();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            switch (tooltipPositioningType)
            {
                case TooltipPositioningType.mousePosition:
                    StartHover(UIExtensionsInputManager.MousePosition + offset, true);
                    break;
                case TooltipPositioningType.mousePositionAndFollow:
                    StartHover(UIExtensionsInputManager.MousePosition + offset, true);
                    hovered = true;
                    StartCoroutine(HoveredMouseFollowingLoop());
                    break;
                case TooltipPositioningType.transformPosition:
                    StartHover(
                        (WorldToScreenIsRequired
                            ? ToolTip.Instance.GuiCamera.WorldToScreenPoint(transform.position)
                            : transform.position) + offset,
                        true
                    );
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHover();
        }

        public void OnSelect(BaseEventData eventData)
        {
            StartHover(
                (WorldToScreenIsRequired
                    ? ToolTip.Instance.GuiCamera.WorldToScreenPoint(transform.position)
                    : transform.position) + offset,
                true
            );
        }

        private IEnumerator HoveredMouseFollowingLoop()
        {
            while (hovered)
            {
                StartHover(UIExtensionsInputManager.MousePosition + offset);
                yield return null;
            }
        }

        private void StartHover(Vector3 position, bool shouldCanvasUpdate = false)
        {
            ToolTip.Instance.SetTooltip(text, position, shouldCanvasUpdate);
        }

        private void StopHover()
        {
            hovered = false;
            ToolTip.Instance.HideTooltip();
        }
    }
}