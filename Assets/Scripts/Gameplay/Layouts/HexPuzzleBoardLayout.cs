using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    public sealed class HexPuzzleBoardLayout : PuzzleBoardLayout
    {
        private const float HexWidthRatio = 128f;
        private const float HexHeightRatio = 111f;

        public override string ShapeId => "hexPuzzle1";

        public override BoardLayoutResult BuildLayout(Rect boardRect, int rowCount, int columnCount)
        {
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount));
            }

            if (columnCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            }

            var columnHeights = BuildColumnHeights(rowCount, columnCount);
            var tallestColumn = 0;
            for (var column = 0; column < columnHeights.Count; column++)
            {
                tallestColumn = Mathf.Max(tallestColumn, columnHeights[column]);
            }

            var horizontalUnits = 1f + (columnCount - 1) * 0.75f;
            var verticalUnits = tallestColumn;
            var scaleByWidth = boardRect.width / (horizontalUnits * HexWidthRatio);
            var scaleByHeight = boardRect.height / (verticalUnits * HexHeightRatio);
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);
            var hexWidth = HexWidthRatio * scale;
            var hexHeight = HexHeightRatio * scale;
            var horizontalStep = hexWidth * 0.75f;
            var verticalStep = hexHeight;
            var totalWidth = hexWidth + (columnCount - 1) * horizontalStep;
            var leftX = -totalWidth * 0.5f + hexWidth * 0.5f;
            var positions = new List<Vector2>(GetSlotCount(columnHeights));

            for (var column = 0; column < columnHeights.Count; column++)
            {
                var columnHeight = columnHeights[column];
                var columnPixelHeight = columnHeight * hexHeight;
                var topY = columnPixelHeight * 0.5f - hexHeight * 0.5f;
                var x = leftX + column * horizontalStep;

                for (var row = 0; row < columnHeight; row++)
                {
                    var y = topY - row * verticalStep;
                    positions.Add(new Vector2(x, y));
                }
            }

            return new BoardLayoutResult(positions, new Vector2(hexWidth, hexHeight));
        }

        private static List<int> BuildColumnHeights(int edgeColumnHeight, int columnCount)
        {
            // edgeColumnHeight = the height of the outer-most columns.
            // Each step toward the center adds one more hexagon.
            // Example: edgeColumnHeight = 4, columnCount = 5  ->  4, 5, 6, 5, 4 (24 total)
            var columnHeights = new List<int>(columnCount);
            var centerIndex = (columnCount - 1) * 0.5f;

            for (var column = 0; column < columnCount; column++)
            {
                var stepsFromCenter = Mathf.RoundToInt(Mathf.Abs(column - centerIndex));
                var maxStepsFromCenter = Mathf.RoundToInt(centerIndex);
                var heightAboveEdge = maxStepsFromCenter - stepsFromCenter;
                columnHeights.Add(Mathf.Max(1, edgeColumnHeight + heightAboveEdge));
            }

            return columnHeights;
        }

        private static int GetSlotCount(List<int> columnHeights)
        {
            var total = 0;
            for (var column = 0; column < columnHeights.Count; column++)
            {
                total += columnHeights[column];
            }

            return total;
        }
    }
}
