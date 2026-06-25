using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "arrowPuzzle10" shape: a regular grid filled with one thick chevron / arrow
    /// piece (the "Adidas arrow", pieceType 0). Columns alternate the arrow's
    /// facing:
    ///   even columns (0, 2, 4 ...) point up   (rotation 0),
    ///   odd  columns (1, 3, 5 ...) point down (rotation 180).
    ///
    /// The up/down columns interlock; the odd (down) columns can be nudged
    /// vertically via <see cref="OddColumnVerticalOffsetRatio"/> so the arrows nest.
    /// All ratio values are tuned by hand in the editor afterwards. Keep the prefab
    /// sprite rotation at 0 so the layout fully controls each slot's rotation.
    /// </summary>
    public sealed class ArrowPuzzle10BoardLayout : PuzzleBoardLayout
    {
        private const int ArrowPiece = 0;

        // Native art size (width x height) of puzzle10Card1.
        private const float ArrowWidthRatio = 87f;
        private const float ArrowHeightRatio = 100f;

        // Spacing between neighbouring slot centres.
        private const float HorizontalStepRatio = 87f;
        private const float VerticalStepRatio = 76f;

        // Extra vertical shift applied to the odd (down-facing) columns so the
        // arrows interlock seamlessly. Negative moves them downward; half the
        // vertical step makes the down arrows nest under the up arrows so the
        // snapped lines continue without breaks.
        private const float OddColumnVerticalOffsetRatio = -VerticalStepRatio * 0.32f;

        // Absolute Z rotation (degrees) per column parity.
        private const float UpColumnRotation = 0f;
        private const float DownColumnRotation = 180f;

        public override string ShapeId => "arrowPuzzle10";

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

            // Reserve the arrow footprint plus the odd-column offset so nothing is
            // clipped by the board edges.
            var horizontalExtent = (columnCount - 1) * HorizontalStepRatio + ArrowWidthRatio;
            var verticalExtent = (rowCount - 1) * VerticalStepRatio + ArrowHeightRatio
                                 + Mathf.Abs(OddColumnVerticalOffsetRatio);
            var scaleByWidth = boardRect.width / horizontalExtent;
            var scaleByHeight = boardRect.height / verticalExtent;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);

            var horizontalStep = HorizontalStepRatio * scale;
            var verticalStep = VerticalStepRatio * scale;
            var oddColumnOffset = OddColumnVerticalOffsetRatio * scale;
            var arrowSize = new Vector2(ArrowWidthRatio, ArrowHeightRatio) * scale;

            var totalWidth = (columnCount - 1) * horizontalStep;
            var totalHeight = (rowCount - 1) * verticalStep;
            var leftX = -totalWidth * 0.5f;
            var topY = totalHeight * 0.5f;

            var slotCount = rowCount * columnCount;
            var positions = new List<Vector2>(slotCount);
            var rotations = new List<float>(slotCount);
            var pieceTypes = new List<int>(slotCount);
            var sizes = new List<Vector2>(slotCount);

            for (var column = 0; column < columnCount; column++)
            {
                var isDownColumn = column % 2 == 1;
                var x = leftX + column * horizontalStep;
                var columnOffset = isDownColumn ? oddColumnOffset : 0f;
                var rotation = isDownColumn ? DownColumnRotation : UpColumnRotation;

                for (var row = 0; row < rowCount; row++)
                {
                    var y = topY - row * verticalStep + columnOffset;
                    positions.Add(new Vector2(x, y));
                    rotations.Add(rotation);
                    pieceTypes.Add(ArrowPiece);
                    sizes.Add(arrowSize);
                }
            }

            return new BoardLayoutResult(positions, arrowSize, rotations, pieceTypes)
            {
                SlotSizes = sizes
            };
        }
    }
}
