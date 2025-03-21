﻿/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;
using System.Linq;
using UnityEngine.UI.Extensions.EasingCore;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample07
{
    internal class Example07 : MonoBehaviour
    {
        [SerializeField] private ScrollView scrollView;
        [SerializeField] private InputField paddingTopInputField;
        [SerializeField] private InputField paddingBottomInputField;
        [SerializeField] private InputField spacingInputField;
        [SerializeField] private InputField dataCountInputField;
        [SerializeField] private InputField selectIndexInputField;
        [SerializeField] private Dropdown alignmentDropdown;

        private void Start()
        {
            scrollView.OnCellClicked(index => selectIndexInputField.text = index.ToString());

            paddingTopInputField.onValueChanged.AddListener(
                _ =>
                    TryParseValue(paddingTopInputField, 0, 999, value => scrollView.PaddingTop = value)
            );
            paddingTopInputField.text = scrollView.PaddingTop.ToString();

            paddingBottomInputField.onValueChanged.AddListener(
                _ =>
                    TryParseValue(paddingBottomInputField, 0, 999, value => scrollView.PaddingBottom = value)
            );
            paddingBottomInputField.text = scrollView.PaddingBottom.ToString();

            spacingInputField.onValueChanged.AddListener(
                _ =>
                    TryParseValue(spacingInputField, 0, 100, value => scrollView.Spacing = value)
            );
            spacingInputField.text = scrollView.Spacing.ToString();

            alignmentDropdown.AddOptions(
                Enum.GetNames(typeof(Alignment)).Select(x => new Dropdown.OptionData(x)).ToList()
            );
            alignmentDropdown.onValueChanged.AddListener(_ => SelectCell());
            alignmentDropdown.value = (int)Alignment.Middle;

            selectIndexInputField.onValueChanged.AddListener(_ => SelectCell());
            selectIndexInputField.text = "10";

            dataCountInputField.onValueChanged.AddListener(
                _ =>
                    TryParseValue(dataCountInputField, 1, 99999, GenerateCells)
            );
            dataCountInputField.text = "20";

            scrollView.JumpTo(10);
        }

        private void TryParseValue(InputField inputField, int min, int max, Action<int> success)
        {
            if (!int.TryParse(inputField.text, out var value)) return;

            if (value < min || value > max)
            {
                inputField.text = Mathf.Clamp(value, min, max).ToString();
                return;
            }

            success(value);
        }

        private void SelectCell()
        {
            if (scrollView.DataCount == 0) return;

            TryParseValue(
                selectIndexInputField,
                0,
                scrollView.DataCount - 1,
                index =>
                    scrollView.ScrollTo(index, 0.3f, Ease.InOutQuint, (Alignment)alignmentDropdown.value)
            );
        }

        private void GenerateCells(int dataCount)
        {
            var items = Enumerable.Range(0, dataCount)
                .Select(i => new ItemData($"Cell {i}"))
                .ToArray();

            scrollView.UpdateData(items);
            SelectCell();
        }
    }
}