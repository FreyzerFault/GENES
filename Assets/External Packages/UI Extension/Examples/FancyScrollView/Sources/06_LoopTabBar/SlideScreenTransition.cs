/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample06
{
    internal class SlideScreenTransition : MonoBehaviour
    {
        private const float Duration = 0.3f; // example purpose, a fixed number, the same with scroll view duration
        [SerializeField] private RectTransform targetTransform;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        private bool shouldAnimate, isOutAnimation;
        private float timer, startX, endX;

        private void Update()
        {
            if (!shouldAnimate) return;

            timer -= Time.deltaTime;

            if (timer > 0)
            {
                UpdatePosition(1f - timer / Duration);
                return;
            }

            shouldAnimate = false;
            graphicRaycaster.enabled = true;

            if (isOutAnimation) gameObject.SetActive(false);

            UpdatePosition(1f);
        }

        public void In(MovementDirection direction) => Animate(direction, false);

        public void Out(MovementDirection direction) => Animate(direction, true);

        private void Animate(MovementDirection direction, bool isOut)
        {
            if (shouldAnimate) return;

            timer = Duration;
            isOutAnimation = isOut;
            shouldAnimate = true;
            graphicRaycaster.enabled = false;

            if (!isOutAnimation) gameObject.SetActive(true);

            switch (direction)
            {
                case MovementDirection.Left:
                    endX = -targetTransform.rect.width;
                    break;

                case MovementDirection.Right:
                    endX = targetTransform.rect.width;
                    break;

                default:
                    Debug.LogWarning("Example only support horizontal direction.");
                    break;
            }

            startX = isOutAnimation ? 0 : -endX;
            endX = isOutAnimation ? endX : 0;

            UpdatePosition(0f);
        }

        private void UpdatePosition(float position)
        {
            var x = Mathf.Lerp(startX, endX, position);
            targetTransform.anchoredPosition = new Vector2(x, targetTransform.anchoredPosition.y);
        }
    }
}