using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "OctaSquarePuzzle7" shape (truncated square tiling, checkerboard layout).
    ///
    /// Octagons and squares alternate on a single regular grid like a
    /// checkerboard. Reading column by column:
    ///   column 0: octa, square, octa, square, ...
    ///   column 1: square, octa, square, octa, ...
    /// i.e. (row + column) even = octagon, odd = square. Every slot is at
    /// rotation 0.
    ///
    ///   pieceType 0 = octagon  (size 174)
    ///   pieceType 1 = square   (size 78)
    ///
    /// columnCount / rowCount describe the full grid (octagons + squares).
    ///
    /// Slot index order (predictable for lockedSlots): row-major over the whole
    /// grid (index = row * columnCount + column), piece types interleaved.
    ///
    /// All ratio values are tuned in the editor afterwards. Keep the prefab
    /// sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class OctaSquarePuzzle7BoardLayout : PuzzleBoardLayout
    {
        private const int OctagonPiece = 0;
        private const int SquarePiece = 1;

        private const float OctagonSizeRatio = 174f;
        private const float SquareSizeRatio = 78f;

        // Distance between adjacent grid cells. Octagon and square share an edge,
        // so the centre-to-centre step is half an octagon plus half a square.
        private const float StepRatio = (OctagonSizeRatio + SquareSizeRatio) * 0.5f;

        // Card effects make the visual vertical gap slightly larger than the
        // mathematical gap. Keep horizontal spacing unchanged and pull rows
        // together by this amount instead of enlarging every square.
        private const float VerticalOverlapRatio = 2f;
        private const float VerticalStepRatio = StepRatio - VerticalOverlapRatio;

        private const float OctagonRotation = 0f;
        private const float SquareRotation = 0f;

        public override string ShapeId => "OctaSquarePuzzle7";

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
            var verticalExtent = (rowCount - 1) * VerticalStepRatio + OctagonSizeRatio;
            var scaleByWidth = boardRect.width / horizontalExtent;
            var scaleByHeight = boardRect.height / verticalExtent;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);

            var horizontalStep = StepRatio * scale;
            var verticalStep = VerticalStepRatio * scale;
            var octagonSize = new Vector2(OctagonSizeRatio, OctagonSizeRatio) * scale;
            var squareSize = new Vector2(SquareSizeRatio, SquareSizeRatio) * scale;

            var totalWidth = (columnCount - 1) * horizontalStep;
            var totalHeight = (rowCount - 1) * verticalStep;
            var leftX = -totalWidth * 0.5f;
            var topY = totalHeight * 0.5f;

            var octagonCount = rowCount * columnCount;
            var slotCount = octagonCount;

            var positions = new List<Vector2>(slotCount);
            var rotations = new List<float>(slotCount);
            var pieceTypes = new List<int>(slotCount);
            var sizes = new List<Vector2>(slotCount);

            // Row-major over the whole grid; piece type alternates by parity.
            for (var row = 0; row < rowCount; row++)
            {
                var y = topY - row * verticalStep;
                for (var column = 0; column < columnCount; column++)
                {
                    var x = leftX + column * horizontalStep;
                    var isOctagon = (row + column) % 2 == 0;

                    positions.Add(new Vector2(x, y));
                    rotations.Add(isOctagon ? OctagonRotation : SquareRotation);
                    pieceTypes.Add(isOctagon ? OctagonPiece : SquarePiece);
                    sizes.Add(isOctagon ? octagonSize : squareSize);
                }
            }

            return new BoardLayoutResult(positions, octagonSize, rotations, pieceTypes)
            {
                SlotSizes = sizes
            };
        }
    }
}
