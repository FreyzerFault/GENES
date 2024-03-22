/// Credit David Gileadi
/// Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/pull-requests/11

using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(Selectable))]
    public class StepperSide :
        UIBehaviour,
        IPointerClickHandler,
        ISubmitHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        internal Sprite cutSprite;

        protected StepperSide()
        {
        }

        private Selectable button => GetComponent<Selectable>();

        private Stepper stepper => GetComponentInParent<Stepper>();

        private bool leftmost => button == stepper.sides[0];

        public virtual void OnDeselect(BaseEventData eventData)
        {
            AdjustSprite(true);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            Press();
            AdjustSprite(false);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            AdjustSprite(true);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            AdjustSprite(false);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();
            AdjustSprite(true);
        }

        private void Press()
        {
            if (!button.IsActive() || !button.IsInteractable()) return;

            if (leftmost)
                stepper.StepDown();
            else
                stepper.StepUp();
        }

        private void AdjustSprite(bool restore)
        {
            var image = button.image;
            if (!image || image.overrideSprite == cutSprite) return;

            if (restore)
                image.overrideSprite = cutSprite;
            else
                image.overrideSprite = Stepper.CutSprite(image.overrideSprite, leftmost);
        }
    }
}