/// Credit RahulOfTheRamanEffect
/// Sourced from - https://forum.unity3d.com/members/rahuloftheramaneffect.773241/

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    ///     Arranges child objects into a non-uniform grid, with fixed column widths and flexible row heights
    /// </summary>
    [AddComponentMenu("Layout/Extensions/Table Layout Group")]
    public class TableLayoutGroup : LayoutGroup
    {
        public enum Corner
        {
            UpperLeft = 0,
            UpperRight = 1,
            LowerLeft = 2,
            LowerRight = 3
        }

        [SerializeField] protected Corner startCorner = Corner.UpperLeft;

        [SerializeField] protected float[] columnWidths = new float[1] { 96f };

        [SerializeField] protected float minimumRowHeight = 32f;

        [SerializeField] protected bool flexibleRowHeight = true;

        [SerializeField] protected float columnSpacing;

        [SerializeField] protected float rowSpacing;

        // Temporarily stores data generated during the execution CalculateLayoutInputVertical for use in SetLayoutVertical
        private float[] preferredRowHeights;

        /// <summary>
        ///     The corner starting from which the cells should be arranged
        /// </summary>
        public Corner StartCorner
        {
            get => startCorner;
            set => SetProperty(ref startCorner, value);
        }

        /// <summary>
        ///     The widths of all the columns in the table
        /// </summary>
        public float[] ColumnWidths
        {
            get => columnWidths;
            set => SetProperty(ref columnWidths, value);
        }

        /// <summary>
        ///     The minimum height for any row in the table
        /// </summary>
        public float MinimumRowHeight
        {
            get => minimumRowHeight;
            set => SetProperty(ref minimumRowHeight, value);
        }

        /// <summary>
        ///     Expand rows to fit the cell with the highest preferred height?
        /// </summary>
        public bool FlexibleRowHeight
        {
            get => flexibleRowHeight;
            set => SetProperty(ref flexibleRowHeight, value);
        }

        /// <summary>
        ///     The horizontal spacing between each cell in the table
        /// </summary>
        public float ColumnSpacing
        {
            get => columnSpacing;
            set => SetProperty(ref columnSpacing, value);
        }

        /// <summary>
        ///     The vertical spacing between each row in the table
        /// </summary>
        public float RowSpacing
        {
            get => rowSpacing;
            set => SetProperty(ref rowSpacing, value);
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear(); // key change - do not restore - false
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float horizontalSize = padding.horizontal;

            // We calculate the actual cell count for cases where the number of children is lesser than the number of columns
            var actualCellCount = Mathf.Min(rectChildren.Count, columnWidths.Length);

            for (var i = 0; i < actualCellCount; i++)
            {
                horizontalSize += columnWidths[i];
                horizontalSize += columnSpacing;
            }

            horizontalSize -= columnSpacing;

            SetLayoutInputForAxis(horizontalSize, horizontalSize, 0, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var columnCount = columnWidths.Length;
            var rowCount = Mathf.CeilToInt(rectChildren.Count / (float)columnCount);

            preferredRowHeights = new float[rowCount];

            float totalMinHeight = padding.vertical;
            float totalPreferredHeight = padding.vertical;

            if (rowCount > 1)
            {
                var heightFromSpacing = (rowCount - 1) * rowSpacing;
                totalMinHeight += heightFromSpacing;
                totalPreferredHeight += heightFromSpacing;
            }

            if (flexibleRowHeight)
            {
                // If flexibleRowHeight is enabled, find the max value for minimum and preferred heights in each row

                float maxMinimumHeightInRow = 0;
                float maxPreferredHeightInRow = 0;

                for (var i = 0; i < rowCount; i++)
                {
                    maxMinimumHeightInRow = minimumRowHeight;
                    maxPreferredHeightInRow = minimumRowHeight;

                    for (var j = 0; j < columnCount; j++)
                    {
                        var childIndex = i * columnCount + j;

                        // Safeguard against tables with incomplete rows
                        if (childIndex == rectChildren.Count) break;

                        maxPreferredHeightInRow = Mathf.Max(
                            LayoutUtility.GetPreferredHeight(rectChildren[childIndex]),
                            maxPreferredHeightInRow
                        );
                        maxMinimumHeightInRow = Mathf.Max(
                            LayoutUtility.GetMinHeight(rectChildren[childIndex]),
                            maxMinimumHeightInRow
                        );
                    }

                    totalMinHeight += maxMinimumHeightInRow;
                    totalPreferredHeight += maxPreferredHeightInRow;

                    // Add calculated row height to a commonly accessible array for reuse in SetLayoutVertical()
                    preferredRowHeights[i] = maxPreferredHeightInRow;
                }
            }
            else
            {
                // If flexibleRowHeight is disabled, then use the minimumRowHeight to calculate vertical layout information
                for (var i = 0; i < rowCount; i++) preferredRowHeights[i] = minimumRowHeight;

                totalMinHeight += rowCount * minimumRowHeight;
                totalPreferredHeight = totalMinHeight;
            }

            totalPreferredHeight = Mathf.Max(totalMinHeight, totalPreferredHeight);
            SetLayoutInputForAxis(totalMinHeight, totalPreferredHeight, 1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            // If no column width is defined, then assign a reasonable default
            if (columnWidths.Length == 0) columnWidths = new float[1] { 0f };

            var columnCount = columnWidths.Length;
            var cornerX = (int)startCorner % 2;

            float startOffset = 0;
            float requiredSizeWithoutPadding = 0;

            // We calculate the actual cell count for cases where the number of children is lesser than the number of columns
            var actualCellCount = Mathf.Min(rectChildren.Count, columnWidths.Length);

            for (var i = 0; i < actualCellCount; i++)
            {
                requiredSizeWithoutPadding += columnWidths[i];
                requiredSizeWithoutPadding += columnSpacing;
            }

            requiredSizeWithoutPadding -= columnSpacing;

            startOffset = GetStartOffset(0, requiredSizeWithoutPadding);

            if (cornerX == 1) startOffset += requiredSizeWithoutPadding;

            var positionX = startOffset;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var currentColumnIndex = i % columnCount;

                // If it's the first cell in the row, reset positionX
                if (currentColumnIndex == 0) positionX = startOffset;

                if (cornerX == 1) positionX -= columnWidths[currentColumnIndex];

                SetChildAlongAxis(rectChildren[i], 0, positionX, columnWidths[currentColumnIndex]);

                if (cornerX == 1)
                    positionX -= columnSpacing;
                else
                    positionX += columnWidths[currentColumnIndex] + columnSpacing;
            }
        }

        public override void SetLayoutVertical()
        {
            var columnCount = columnWidths.Length;
            var rowCount = preferredRowHeights.Length;

            var cornerY = (int)startCorner / 2;

            float startOffset = 0;
            float requiredSizeWithoutPadding = 0;

            for (var i = 0; i < rowCount; i++) requiredSizeWithoutPadding += preferredRowHeights[i];

            if (rowCount > 1) requiredSizeWithoutPadding += (rowCount - 1) * rowSpacing;

            startOffset = GetStartOffset(1, requiredSizeWithoutPadding);

            if (cornerY == 1) startOffset += requiredSizeWithoutPadding;

            var positionY = startOffset;

            for (var i = 0; i < rowCount; i++)
            {
                if (cornerY == 1) positionY -= preferredRowHeights[i];

                for (var j = 0; j < columnCount; j++)
                {
                    var childIndex = i * columnCount + j;

                    // Safeguard against tables with incomplete rows
                    if (childIndex == rectChildren.Count) break;

                    SetChildAlongAxis(rectChildren[childIndex], 1, positionY, preferredRowHeights[i]);
                }

                if (cornerY == 1)
                    positionY -= rowSpacing;
                else
                    positionY += preferredRowHeights[i] + rowSpacing;
            }

            // Set preferredRowHeights to null to free memory
            preferredRowHeights = null;
        }
    }
}