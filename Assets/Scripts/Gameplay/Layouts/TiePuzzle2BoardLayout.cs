using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    public sealed class TiePuzzle2BoardLayout : PuzzleBoardLayout
    {
        // Card art is 205 x 145. Cards are placed either flat (0 degrees) or
        // rotated 90 degrees in a checkerboard pattern so they interlock.
        private const float CardWidthRatio = 205f;
        private const float CardHeightRatio = 145f;

        // Distance between neighbouring card centres (tune in editor afterwards).
        private const float StepRatio = 140f;

        public override string ShapeId => "tiePuzzle2";

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

            // The longest card dimension (205) can stick out on either axis once
            // some cards are rotated, so reserve that much extra room when fitting.
            var horizontalExtent = (columnCount - 1) * StepRatio + CardWidthRatio;
            var verticalExtent = (rowCount - 1) * StepRatio + CardWidthRatio;
            var scaleByWidth = boardRect.width / horizontalExtent;
            var scaleByHeight = boardRect.height / verticalExtent;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);

            var step = StepRatio * scale;
            var cardWidth = CardWidthRatio * scale;
            var cardHeight = CardHeightRatio * scale;

            var totalWidth = (columnCount - 1) * step;
            var totalHeight = (rowCount - 1) * step;
            var leftX = -totalWidth * 0.5f;
            var topY = totalHeight * 0.5f;

            var slotCount = rowCount * columnCount;
            var positions = new List<Vector2>(slotCount);
            var rotations = new List<float>(slotCount);

            for (var row = 0; row < rowCount; row++)
            {
                var y = topY - row * step;
                for (var column = 0; column < columnCount; column++)
                {
                    var x = leftX + column * step;
                    positions.Add(new Vector2(x, y));
                    rotations.Add((row + column) % 2 == 0 ? 0f : 90f);
                }
            }

            return new BoardLayoutResult(positions, new Vector2(cardWidth, cardHeight), rotations);
        }
    }
}
