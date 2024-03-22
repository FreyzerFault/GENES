/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample03
{
    internal class Cell : FancyCell<ItemData, Context>
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Text message;
        [SerializeField] private Text messageLarge;
        [SerializeField] private Image image;
        [SerializeField] private Image imageLarge;
        [SerializeField] private Button button;

        // GameObject が非アクティブになると Animator がリセットされてしまうため
        // 現在位置を保持しておいて OnEnable のタイミングで現在位置を再設定します
        private float currentPosition;

        private void Start()
        {
            button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
        }

        private void OnEnable() => UpdatePosition(currentPosition);

        public override void UpdateContent(ItemData itemData)
        {
            message.text = itemData.Message;
            messageLarge.text = Index.ToString();

            var selected = Context.SelectedIndex == Index;
            imageLarge.color = image.color = selected
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