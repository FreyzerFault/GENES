/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample09
{
    internal class Example09 : MonoBehaviour
    {
        [SerializeField] private ScrollView scrollView;

        private readonly ItemData[] itemData =
        {
            new(
                "FancyScrollView",
                "A scrollview component that can implement highly flexible animation.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/00.png"
            ),
            new(
                "01_Basic",
                "Example of simplest implementation.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/01.png"
            ),
            new(
                "02_FocusOn",
                "Example of focusing on the left and right cells with buttons.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/02.png"
            ),
            new(
                "03_InfiniteScroll",
                "Example of infinite scroll implementation.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/03.png"
            ),
            new(
                "04_Metaball",
                "Example of metaball implementation using shaders.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/04.png"
            ),
            new(
                "05_Voronoi",
                "Example of voronoi implementation using shaders.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/05.png"
            ),
            new(
                "06_LoopTabBar",
                "Example of switching screens with tabs.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/06.png"
            ),
            new(
                "07_ScrollRect",
                "Example of ScrollRect style implementation with scroll bar.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/07.png"
            ),
            new(
                "08_GridView",
                "Example of grid layout implementation.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/08.png"
            ),
            new(
                "09_LoadTexture",
                "Example of load texture implementation.",
                "https://setchi.jp/FancyScrollView/09_LoadTexture/Images/09.png"
            )
        };

        private void Start()
        {
            scrollView.UpdateData(itemData);
        }
    }
}