using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "HexTrianglePuzzle6" shape: the trihexagonal tiling (3.6.3.6).
    ///
    /// Pointy-top hexagons touch corner-to-corner on a triangular lattice and the
    /// gaps between every three hexagons are filled by equilateral triangles
    /// (no squares). With edge a = 90:
    ///   pieceType 0 = hexagon  (corner-to-corner 180, edge 90)
    ///   pieceType 1 = triangle (edge 90, drawn 78 wide x 90 tall, apex horizontal)
    ///
    /// Hexagon centres are laid out column by column. Column heights grow by one
    /// toward the centre, so adjacent columns differ by one hexagon and therefore
    /// interlock half a vertical step (the triangular lattice).
    ///
    /// Lattice steps (edge a):
    ///   colStep (x) = a*sqrt(3)        (between hex columns)
    ///   rowStep (y) = 2*a              (between hexes in a column)
    ///
    /// Fill triangles are only emitted where they are shared between hexagons, so
    /// the outer silhouette never has loose triangles sticking out. Every triangle
    /// is the centroid of three mutually adjacent hexagons and has one of two
    /// horizontal orientations (apex pointing left or right).
    ///
    /// Slot index order (predictable for lockedSlots):
    ///   all hexagons (column-major), then all triangles.
    ///
    /// Keep prefab sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class HexTrianglePuzzle6BoardLayout : PuzzleBoardLayout
    {
        private const int HexagonPiece = 0;
        private const int TrianglePiece = 1;

        private const float EdgeRatio = 90f;      // shared edge length (a)

        // Pointy-top hexagon bounding box (Preserve Aspect): width = a*sqrt(3),
        // height = 2a. With a = 90 this is 156 x 180.
        private const float HexWidthRatio = 156f;  // a*sqrt(3)
        private const float HexHeightRatio = 180f; // 2a

        // Triangle sprite bounding box (apex horizontal): 78 wide x 90 tall.
        private const float TriangleWidthRatio = 78f;   // a*sqrt(3)/2
        private const float TriangleHeightRatio = 90f;  // a

        // Rotation (degrees) for the two fill-triangle orientations, assuming the
        // triangle sprite is authored pointing to the right at rotation 0.
        private const float TriangleApexRightRotation = 180f;
        private const float TriangleApexLeftRotation = 0f;

        // Editor-tunable overrides in case the sprite's default facing differs.
        private const float TriangleRotationOffset = 0f;

        // Signed distance from the fill triangle's geometric centroid to the
        // triangle sprite's pivot, measured along the edge normal (apex)
        // direction. For an equilateral-triangle sprite whose pivot sits at the
        // bounding-box centre, the pivot is offset toward the apex by
        // height/6 (= TriangleWidthRatio/6). Set to 0 if the sprite pivot already
        // sits on the centroid; flip the sign if the drift goes the wrong way.
        private const float TriangleCentroidApexOffset = TriangleWidthRatio / 6f;

        public override string ShapeId => "HexTrianglePuzzle6";

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

            const float a = EdgeRatio;
            var colStep = a * Mathf.Sqrt(3f); // horizontal hex spacing
            var rowStep = 2f * a;             // vertical hex spacing

            // 1) Build hexagon centres, column by column, centred per column so
            //    that columns whose heights differ by one interlock half a step.
            var columnHeights = BuildColumnHeights(rowCount, columnCount);
            var hexPositions = new List<Vector2>();

            for (var column = 0; column < columnCount; column++)
            {
                var height = columnHeights[column];
                var x = (column - (columnCount - 1) * 0.5f) * colStep;

                for (var i = 0; i < height; i++)
                {
                    var y = (i - (height - 1) * 0.5f) * rowStep;
                    var position = new Vector2(x, y);
                    hexPositions.Add(position);
                }
            }

            // 2) The six neighbour directions of a pointy-top hexagon centre.
            var up = new Vector2(0f, rowStep);                  // 90
            var down = new Vector2(0f, -rowStep);               // 270
            var upRight = new Vector2(colStep, rowStep * 0.5f); // 30
            var upLeft = new Vector2(-colStep, rowStep * 0.5f); // 150
            var downRight = new Vector2(colStep, -rowStep * 0.5f);
            var downLeft = new Vector2(-colStep, -rowStep * 0.5f);

            // Consecutive neighbour pairs around the hexagon and the apex direction
            // of the triangle that fills each gap (apex left = 180, apex right = 0).
            var neighbourPairs = new[]
            {
                (up, upRight, TriangleApexLeftRotation),
                (upRight, downRight, TriangleApexRightRotation),
                (downRight, down, TriangleApexLeftRotation),
                (down, downLeft, TriangleApexRightRotation),
                (downLeft, upLeft, TriangleApexLeftRotation),
                (upLeft, up, TriangleApexRightRotation),
            };

            var trianglePositions = new List<Vector2>();
            var triangleRotations = new List<float>();
            var seenTriangles = new HashSet<long>();

            // A triangle is emitted for every one of the six edges of every
            // hexagon, regardless of whether the neighbouring hexagons exist.
            // This fills the outer edges too, producing the spiky star
            // silhouette. Shared (interior) triangles are deduplicated by their
            // centroid so they are only created once.
            for (var index = 0; index < hexPositions.Count; index++)
            {
                var centre = hexPositions[index];

                foreach (var (offsetA, offsetB, apexRotation) in neighbourPairs)
                {
                    var neighbourA = centre + offsetA;
                    var neighbourB = centre + offsetB;

                    AddTriangle(centre, neighbourA, neighbourB, apexRotation,
                        trianglePositions, triangleRotations, seenTriangles);
                }
            }

            // 3) Pack groups in order: hexagons, triangles.
            var positions = new List<Vector2>();
            var rotations = new List<float>();
            var pieceTypes = new List<int>();
            var sizeRatios = new List<Vector2>();

            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                hexPositions, null, HexagonPiece, new Vector2(HexWidthRatio, HexHeightRatio));
            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                trianglePositions, triangleRotations, TrianglePiece, new Vector2(TriangleWidthRatio, TriangleHeightRatio));

            // 4) Centre on the board and scale to fit.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < positions.Count; index++)
            {
                min = Vector2.Min(min, positions[index]);
                max = Vector2.Max(max, positions[index]);
            }

            var contentWidth = (max.x - min.x) + HexWidthRatio;
            var contentHeight = (max.y - min.y) + HexHeightRatio;
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

            var hexCellSize = new Vector2(HexWidthRatio, HexHeightRatio) * scale;
            return new BoardLayoutResult(finalPositions, hexCellSize, rotations, pieceTypes)
            {
                SlotSizes = finalSizes
            };
        }

        private static List<int> BuildColumnHeights(int edgeColumnHeight, int columnCount)
        {
            // Outer columns are the shortest; each step toward the centre adds one
            // hexagon. columnCount = 3, edge = 3 -> 3, 4, 3.
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

        private static void AddTriangle(
            Vector2 a, Vector2 b, Vector2 c, float apexRotation,
            List<Vector2> positions, List<float> rotations, HashSet<long> seen)
        {
            var centroid = (a + b + c) / 3f;
            if (!seen.Add(KeyFor(centroid)))
            {
                return;
            }

            var rotation = NormalizeAngle(apexRotation + TriangleRotationOffset, 360f);

            // The two triangle orientations both have a vertical base and a
            // horizontal apex. With Preserve Aspect the sprite is centred on its
            // bounding-box centre, which sits TriangleWidthRatio/6 toward the apex
            // from the geometric centroid. Find the apex direction (purely
            // horizontal): the apex points away from the hexagon whose centre is
            // the horizontal outlier (largest |x| relative to the centroid).
            var relA = a - centroid;
            var relB = b - centroid;
            var relC = c - centroid;
            var outlierX = relA.x;
            if (Mathf.Abs(relB.x) > Mathf.Abs(outlierX)) outlierX = relB.x;
            if (Mathf.Abs(relC.x) > Mathf.Abs(outlierX)) outlierX = relC.x;
            var apexSign = outlierX > 0f ? -1f : 1f;
            var offset = new Vector2(apexSign * TriangleCentroidApexOffset, 0f);

            positions.Add(centroid + offset);
            rotations.Add(rotation);
        }

        private static void AppendGroup(
            List<Vector2> positions, List<float> rotations, List<int> pieceTypes, List<Vector2> sizes,
            List<Vector2> groupPositions, List<float> groupRotations, int pieceType, Vector2 sizeRatio)
        {
            for (var index = 0; index < groupPositions.Count; index++)
            {
                positions.Add(groupPositions[index]);
                rotations.Add(groupRotations != null ? groupRotations[index] : 0f);
                pieceTypes.Add(pieceType);
                sizes.Add(sizeRatio);
            }
        }

        private static long KeyFor(Vector2 position)
        {
            var x = Mathf.RoundToInt(position.x);
            var y = Mathf.RoundToInt(position.y);
            return ((long)x << 32) ^ (uint)y;
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
