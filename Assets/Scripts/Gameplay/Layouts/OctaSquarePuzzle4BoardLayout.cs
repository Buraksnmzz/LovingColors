using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "OctaSquarePuzzle4" shape (truncated square tiling).
    ///
    /// Octagons sit on a regular grid and snap edge-to-edge through their four
    /// short straight edges. Their four longer diagonal edges leave a small
    /// square gap between every block of four octagons; a small equilateral
    /// deltoid (a diamond) drops exactly into each of those gaps.
    ///
    ///   pieceType 0 = octagon  (size 148)
    ///   pieceType 1 = deltoid  (size 100)
    ///
    /// columnCount / rowCount describe the OCTAGON grid. Deltoids are placed on
    /// the (columnCount-1) x (rowCount-1) inner grid, offset half a step on both
    /// axes so they land in the diagonal gaps.
    ///
    /// Slot index order (predictable for lockedSlots):
    ///   first ALL octagons row-major (0 .. rows*cols-1),
    ///   then  ALL deltoids row-major.
    ///
    /// All ratio/rotation values are tuned in the editor afterwards. Keep the
    /// prefab sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class OctaSquarePuzzle4BoardLayout : PuzzleBoardLayout
    {
        private const int OctagonPiece = 0;
        private const int DeltoidPiece = 1;

        private const float OctagonSizeRatio = 148f;
        private const float DeltoidSizeRatio = 100f;

        // Octagon centres are one octagon apart so their straight edges touch.
        private const float StepRatio = OctagonSizeRatio;

        private const float OctagonRotation = 0f;
        private const float DeltoidRotation = 0f;

        public override string ShapeId => "OctaSquarePuzzle4";

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

            // The octagons define the footprint; reserve one octagon of padding so
            // nothing clips at the edges.
            var horizontalExtent = (columnCount - 1) * StepRatio + OctagonSizeRatio;
            var verticalExtent = (rowCount - 1) * StepRatio + OctagonSizeRatio;
            var scaleByWidth = boardRect.width / horizontalExtent;
            var scaleByHeight = boardRect.height / verticalExtent;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);

            var step = StepRatio * scale;
            var octagonSize = new Vector2(OctagonSizeRatio, OctagonSizeRatio) * scale;
            var deltoidSize = new Vector2(DeltoidSizeRatio, DeltoidSizeRatio) * scale;

            var totalWidth = (columnCount - 1) * step;
            var totalHeight = (rowCount - 1) * step;
            var leftX = -totalWidth * 0.5f;
            var topY = totalHeight * 0.5f;

            var octagonCount = rowCount * columnCount;
            var deltoidCount = Mathf.Max(0, rowCount - 1) * Mathf.Max(0, columnCount - 1);
            var slotCount = octagonCount + deltoidCount;

            var positions = new List<Vector2>(slotCount);
            var rotations = new List<float>(slotCount);
            var pieceTypes = new List<int>(slotCount);
            var sizes = new List<Vector2>(slotCount);

            // Octagons (row-major).
            for (var row = 0; row < rowCount; row++)
            {
                var y = topY - row * step;
                for (var column = 0; column < columnCount; column++)
                {
                    var x = leftX + column * step;
                    positions.Add(new Vector2(x, y));
                    rotations.Add(OctagonRotation);
                    pieceTypes.Add(OctagonPiece);
                    sizes.Add(octagonSize);
                }
            }

            // Deltoids fill the gaps at the centre of each 2x2 octagon block.
            for (var row = 0; row < rowCount - 1; row++)
            {
                var y = topY - (row + 0.5f) * step;
                for (var column = 0; column < columnCount - 1; column++)
                {
                    var x = leftX + (column + 0.5f) * step;
                    positions.Add(new Vector2(x, y));
                    rotations.Add(DeltoidRotation);
                    pieceTypes.Add(DeltoidPiece);
                    sizes.Add(deltoidSize);
                }
            }

            return new BoardLayoutResult(positions, octagonSize, rotations, pieceTypes)
            {
                SlotSizes = sizes
            };
        }
    }
}
