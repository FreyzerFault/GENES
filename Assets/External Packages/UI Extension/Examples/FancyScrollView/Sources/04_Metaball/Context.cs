﻿/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample04
{
    internal class Context
    {
        // xy = cell position, z = data index, w = scale
        public Vector4[] CellState = new Vector4[1];

        // Cell -> ScrollView
        public Action<int> OnCellClicked;
        public int SelectedIndex = -1;

        // ScrollView -> Cell
        public Action UpdateCellState;

        public void SetCellState(int cellIndex, int dataIndex, float x, float y, float scale)
        {
            var size = cellIndex + 1;
            if (size > CellState.Length) Array.Resize(ref CellState, size);

            CellState[cellIndex].x = x;
            CellState[cellIndex].y = y;
            CellState[cellIndex].z = dataIndex;
            CellState[cellIndex].w = scale;
        }
    }
}