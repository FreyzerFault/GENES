﻿/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample02
{
    internal class Cell : FancyCell<ItemData, Context>
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Text message;
        [SerializeField] private Image image;
        [SerializeField] private Button button;

        // GameObject が非アクティブになると Animator がリセットされてしまうため
        // 現在位置を保持しておいて OnEnable のタイミングで現在位置を再設定します
        private float currentPosition;

        private void OnEnable() => UpdatePosition(currentPosition);

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

        public override void UpdatePosition(float position)
        {
            currentPosition = position;

            if (animator.isActiveAndEnabled) animator.Play(AnimatorHash.Scroll, -1, position);

            animator.speed = 0;
        }

        private static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }
    }
}