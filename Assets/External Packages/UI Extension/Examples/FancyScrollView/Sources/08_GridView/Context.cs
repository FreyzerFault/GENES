/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample08
{
    internal class Context : FancyGridViewContext
    {
        public Action<int> OnCellClicked;
        public int SelectedIndex = -1;
    }
}