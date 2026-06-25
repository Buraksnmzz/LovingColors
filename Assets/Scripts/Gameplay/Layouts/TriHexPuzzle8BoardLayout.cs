using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "triHexPuzzle8" shape: a flat-top hexagonal honeycomb (exactly like
    /// <see cref="HexPuzzleBoardLayout"/>: 3x3 -> 3-4-3 = 10 hexagons) where every
    /// hexagon is cut into its six equilateral triangles. A 3x3 board therefore
    /// contains 10 * 6 = 60 triangles.
    ///
    /// Each hexagon has circumradius R. Its six vertices sit at 0,60,...,300 deg.
    /// Triangle k spans the hexagon centre and the two consecutive vertices
    /// V_k, V_(k+1); it is equilateral with side R, apex at the hexagon centre and
    /// its base on the outer hexagon edge (edge-midpoint normal at 30 + 60k deg).
    ///
    /// The triangle sprite is authored pointing to the right (apex at +x) at
    /// rotation 0, with bounding box width = R*sqrt(3)/2 (apex-to-base height) and
    /// height = R (base length). Since the apex points toward the hexagon centre,
    /// each triangle is rotated to (30 + 60k + 180) deg.
    ///
    /// Slot index order (predictable for lockedSlots): hexagon by hexagon, and
    /// within each hexagon the six triangles in increasing k (0..5).
    ///
    /// Keep prefab sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class TriHexPuzzle8BoardLayout : PuzzleBoardLayout
    {
        private const int TrianglePiece = 0;

        private const float Radius = 100f;              // hexagon circumradius (R)
        private const int TrianglesPerHexagon = 6;

        // Equilateral triangle (side R) sprite bounding box, apex horizontal.
        private static readonly float TriangleWidthRatio = Radius * Mathf.Sqrt(3f) / 2f; // apex-to-base height
        private const float TriangleHeightRatio = Radius;                                 // base length

        // Sprite pivot sits at the bounding-box centre, which is height/6 toward
        // the apex from the geometric centroid (centroid is 1/3 from base, bbox
        // centre is 1/2 from base). Offset the placement so the drawn triangle's
        // centroid lands on the true centroid.
        private static readonly float CentroidApexOffset = (Radius * Mathf.Sqrt(3f) / 2f) / 6f;

        public override string ShapeId => "triHexPuzzle8";

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

            const float r = Radius;
            var horizontalStep = r * 1.5f;          // flat-top hex column spacing
            var verticalStep = r * Mathf.Sqrt(3f);  // flat-top hex row spacing

            var columnHeights = BuildColumnHeights(rowCount, columnCount);
            var hexCentres = new List<Vector2>();

            for (var column = 0; column < columnCount; column++)
            {
                var height = columnHeights[column];
                var x = (column - (columnCount - 1) * 0.5f) * horizontalStep;

                for (var i = 0; i < height; i++)
                {
                    var y = (i - (height - 1) * 0.5f) * verticalStep;
                    hexCentres.Add(new Vector2(x, y));
                }
            }

            // Flat-top hexagon vertices at 0,60,...,300 degrees (counter-clockwise).
            var vertexOffsets = new Vector2[6];
            for (var k = 0; k < 6; k++)
            {
                var angle = k * 60f * Mathf.Deg2Rad;
                vertexOffsets[k] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }

            var positions = new List<Vector2>(hexCentres.Count * TrianglesPerHexagon);
            var rotations = new List<float>(hexCentres.Count * TrianglesPerHexagon);
            var pieceTypes = new List<int>(hexCentres.Count * TrianglesPerHexagon);
            var sizeRatios = new List<Vector2>(hexCentres.Count * TrianglesPerHexagon);
            var triangleSize = new Vector2(TriangleWidthRatio, TriangleHeightRatio);

            foreach (var centre in hexCentres)
            {
                for (var k = 0; k < 6; k++)
                {
                    var vA = centre + vertexOffsets[k];
                    var vB = centre + vertexOffsets[(k + 1) % 6];
                    var centroid = (centre + vA + vB) / 3f;

                    // Apex points from the centroid toward the hexagon centre.
                    var apexDir = (centre - centroid);
                    if (apexDir.sqrMagnitude > Mathf.Epsilon)
                    {
                        apexDir.Normalize();
                    }

                    var apexAngle = Mathf.Atan2(apexDir.y, apexDir.x) * Mathf.Rad2Deg;
                    positions.Add(centroid + apexDir * CentroidApexOffset);
                    rotations.Add(NormalizeAngle(apexAngle, 360f));
                    pieceTypes.Add(TrianglePiece);
                    sizeRatios.Add(triangleSize);
                }
            }

            // Centre on the board and scale to fit.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var position in positions)
            {
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var contentWidth = (max.x - min.x) + r;
            var contentHeight = (max.y - min.y) + r;
            var scaleByWidth = boardRect.width / contentWidth;
            var scaleByHeight = boardRect.height / contentHeight;
            var scale = Mathf.Min(scaleByWidth, scaleByHeight);
            var contentCentre = (min + max) * 0.5f;

            var finalPositions = new List<Vector2>(positions.Count);
            var finalSizes = new List<Vector2>(positions.Count);
            for (var index = 0; index < positions.Count; index++)
            {
                finalPositions.Add((positions[index] - contentCentre) * scale);
                finalSizes.Add(sizeRatios[index] * scale);
            }

            var cellSize = triangleSize * scale;
            return new BoardLayoutResult(finalPositions, cellSize, rotations, pieceTypes)
            {
                SlotSizes = finalSizes
            };
        }

        private static List<int> BuildColumnHeights(int edgeColumnHeight, int columnCount)
        {
            // Outer columns are the shortest; each step toward the centre adds one
            // hexagon. columnCount = 3, edge = 3 -> 3, 4, 3 (10 hexagons).
            var columnHeights = new List<int>(columnCount);
            var centerIndex = (columnCount - 1) * 0.5f;
            var maxStepsFromCenter = Mathf.RoundToInt(centerIndex);

            for (var column = 0; column < columnCount; column++)
            {
                var stepsFromCenter = Mathf.RoundToInt(Mathf.Abs(column - centerIndex));
                var heightAboveEdge = maxStepsFromCenter - stepsFromCenter;
                columnHeights.Add(Mathf.Max(1, edgeColumnHeight + heightAboveEdge));
            }

            return columnHeights;
        }

        private static float NormalizeAngle(float angle, float period)
        {
            var result = angle % period;
            if (result < 0f)
            {
                result += period;
            }

            return result;
        }
    }
}
