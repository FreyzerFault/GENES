﻿/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System.Linq;
using UnityEngine.UI.Extensions.EasingCore;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample09
{
    internal class Cell : FancyCell<ItemData>
    {
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private RawImage image;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;
        private readonly EasingFunction alphaEasing = Easing.Get(Ease.OutQuint);

        private ItemData data;

        public override void UpdateContent(ItemData itemData)
        {
            data = itemData;
            image.texture = null;

            TextureLoader.Load(
                itemData.Url,
                result =>
                {
                    if (image == null || result.Url != data.Url) return;

                    image.texture = result.Texture;
                }
            );

            title.text = itemData.Title;
            description.text = itemData.Description;

            UpdateSibling();
        }

        private void UpdateSibling()
        {
            var cells = transform.parent.Cast<Transform>()
                .Select(t => t.GetComponent<Cell>())
                .Where(cell => cell.IsVisible);

            if (Index == cells.Min(x => x.Index)) transform.SetAsLastSibling();

            if (Index == cells.Max(x => x.Index)) transform.SetAsFirstSibling();
        }

        public override void UpdatePosition(float t)
        {
            const float popAngle = -15;
            const float slideAngle = 25;

            const float popSpan = 0.75f;
            const float slideSpan = 0.25f;

            t = 1f - t;

            var pop = Mathf.Min(popSpan, t) / popSpan;
            var slide = Mathf.Max(0, t - popSpan) / slideSpan;

            transform.localRotation = t < popSpan
                ? Quaternion.Euler(0, 0, popAngle * (1f - pop))
                : Quaternion.Euler(0, 0, slideAngle * slide);

            transform.localPosition = Vector3.left * 500f * slide;

            canvasGroup.alpha = alphaEasing(1f - slide);

            background.color = Color.Lerp(Color.gray, Color.white, pop);
        }
    }
}