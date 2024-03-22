/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample07
{
    internal class Context : FancyScrollRectContext
    {
        public Action<int> OnCellClicked;
        public int SelectedIndex = -1;
    }
}