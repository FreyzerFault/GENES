/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample09
{
    internal class ItemData
    {
        public ItemData(string title, string description, string url)
        {
            Title = title;
            Description = description;
            Url = url;
        }

        public string Title { get; }
        public string Description { get; }
        public string Url { get; }
    }
}