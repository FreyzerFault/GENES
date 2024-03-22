/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System.Linq;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample04
{
    internal class Example04 : MonoBehaviour
    {
        [SerializeField] private ScrollView scrollView;
        [SerializeField] private Button prevCellButton;
        [SerializeField] private Button nextCellButton;
        [SerializeField] private Text selectedItemInfo;

        private void Start()
        {
            prevCellButton.onClick.AddListener(scrollView.SelectPrevCell);
            nextCellButton.onClick.AddListener(scrollView.SelectNextCell);
            scrollView.OnSelectionChanged(OnSelectionChanged);

            var items = Enumerable.Range(0, 20)
                .Select(i => new ItemData($"Cell {i}"))
                .ToList();

            scrollView.UpdateData(items);
            scrollView.UpdateSelection(10);
            scrollView.JumpTo(10);
        }

        private void OnSelectionChanged(int index)
        {
            selectedItemInfo.text = $"Selected item info: index {index}";
        }
    }
}