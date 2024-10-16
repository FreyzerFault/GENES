﻿/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample07
{
    internal class Cell : FancyScrollRectCell<ItemData, Context>
    {
        [SerializeField] private Text message;
        [SerializeField] private Image image;
        [SerializeField] private Button button;

        public override void Initialize()
        {
            button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
        }

        public override void UpdateContent(ItemData itemData)
        {
            message.text = itemData.Message;

            var selected = Context.SelectedIndex == Index;
            image.color = selected
                ? new Color32(0, 255, 255, 100)
                : new Color32(255, 255, 255, 77);
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            base.UpdatePosition(normalizedPosition, localPosition);

            var wave = Mathf.Sin(normalizedPosition * Mathf.PI * 2) * 65;
            transform.localPosition += Vector3.right * wave;
        }
    }
}