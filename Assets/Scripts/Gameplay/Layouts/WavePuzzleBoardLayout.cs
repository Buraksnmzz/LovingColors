using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "wavePuzzle" shape. A regular grid where pieces alternate between a square
    /// (pieceType 0) and a deltoid (pieceType 1) in a checkerboard fashion, with
    /// each row flipping the pattern so the shapes interlock like a wave.
    ///
    /// Pattern (matches the reference art):
    ///   Row 0: square, deltoid, square, deltoid ...
    ///   Row 1: deltoid, square,  deltoid, square ...
    ///   Row 2 == Row 0, Row 3 == Row 1 ...
    ///
    /// pieceType = ((row + column) even) ? square(0) : deltoid(1)
    ///
    /// All ratio/rotation values below are tuned by hand in the editor afterwards.
    /// Prefabs should keep their sprite rotation at 0 so the layout fully controls
    /// each slot's rotation.
    /// </summary>
    public sealed class WavePuzzleBoardLayout : PuzzleBoardLayout
    {
        private const int SquarePiece = 0;
        private const int DeltoidPiece = 1;

        // Native art sizes (square edge / deltoid width x height).
        private const float SquareSizeRatio = 138f;
        private const float DeltoidWidthRatio = 98f;
        private const float DeltoidHeightRatio = 256f;

        // Spacing between neighbouring slot centres.
        private const float HorizontalStepRatio = 126f;
        private const float VerticalStepRatio = 126f;

        // Absolute Z rotation (degrees) per piece, depending on the row parity.
        private const float SquareRotationEvenRow = 24f;
        private const float SquareRotationOddRow = 66f;
        private const float DeltoidRotationEvenRow = 45f;
        private const float DeltoidRotationOddRow = 135f;

        public override string ShapeId => "wavePuzzle3";

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

            // Reserve the largest piece footprint (the deltoid height) on both axes
            // so rotated pieces never get clipped by the board edges.
            var maxPieceExtent = DeltoidHeightRatio;
            var horizontalExtent = (columnCount - 1) * HorizontalStepRatio + maxPieceExtent;
            var verticalExtent = (rowCount - 1) * VerticalStepRatio + maxPieceExtent;
            var scaleByWidth = boardRect.width / horizontalExtent;
            var scaleByHeight = boardRect.height / verticalExtent;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);

            var horizontalStep = HorizontalStepRatio * scale;
            var verticalStep = VerticalStepRatio * scale;
            var squareSize = new Vector2(SquareSizeRatio, SquareSizeRatio) * scale;
            var deltoidSize = new Vector2(DeltoidWidthRatio, DeltoidHeightRatio) * scale;

            var totalWidth = (columnCount - 1) * horizontalStep;
            var totalHeight = (rowCount - 1) * verticalStep;
            var leftX = -totalWidth * 0.5f;
            var topY = totalHeight * 0.5f;

            var slotCount = rowCount * columnCount;
            var positions = new List<Vector2>(slotCount);
            var rotations = new List<float>(slotCount);
            var pieceTypes = new List<int>(slotCount);
            var sizes = new List<Vector2>(slotCount);

            for (var row = 0; row < rowCount; row++)
            {
                var isEvenRow = row % 2 == 0;
                var y = topY - row * verticalStep;

                for (var column = 0; column < columnCount; column++)
                {
                    var x = leftX + column * horizontalStep;
                    var isSquare = (row + column) % 2 == 0;

                    positions.Add(new Vector2(x, y));

                    if (isSquare)
                    {
                        pieceTypes.Add(SquarePiece);
                        sizes.Add(squareSize);
                        rotations.Add(isEvenRow ? SquareRotationEvenRow : SquareRotationOddRow);
                    }
                    else
                    {
                        pieceTypes.Add(DeltoidPiece);
                        sizes.Add(deltoidSize);
                        rotations.Add(isEvenRow ? DeltoidRotationEvenRow : DeltoidRotationOddRow);
                    }
                }
            }

            return new BoardLayoutResult(positions, squareSize, rotations, pieceTypes)
            {
                SlotSizes = sizes
            };
        }
    }
}
