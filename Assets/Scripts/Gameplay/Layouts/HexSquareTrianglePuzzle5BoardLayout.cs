using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "HexSquareTrianglePuzzle5" shape: the rhombitrihexagonal tiling (3.4.6.4).
    ///
    /// Flat-top hexagons are arranged in vertical columns. Column heights grow by
    /// one toward the centre (like the hex puzzle), so e.g. columnCount = 3,
    /// rowCount = 3 produces hexagon columns of 3, 4, 3. Because adjacent columns
    /// differ in height by one, their hexagons naturally interlock half a step
    /// apart (a triangular lattice).
    ///
    /// Fill pieces are only emitted where they are shared BETWEEN hexagons, so the
    /// outer silhouette never has loose squares/triangles sticking out:
    ///   pieceType 0 = hexagon  (corner-to-corner 180, edge 90)
    ///   pieceType 1 = square   (edge 90)   -> needs 2 neighbouring hexagons
    ///   pieceType 2 = triangle (edge 90)   -> needs 3 mutually adjacent hexagons
    ///
    /// Derived angles (edge a = 90):
    ///   vertical-neighbour square  -> 0 degrees
    ///   diagonal-neighbour squares -> 30 / 60 degrees
    ///   triangles                  -> 30 / 90 degrees (two orientations)
    ///
    /// Slot index order (predictable for lockedSlots):
    ///   all hexagons (column-major), then all squares, then all triangles.
    ///
    /// Keep prefab sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class HexSquareTrianglePuzzle5BoardLayout : PuzzleBoardLayout
    {
        private const int HexagonPiece = 0;
        private const int SquarePiece = 1;
        private const int TrianglePiece = 2;

        private const float EdgeRatio = 90f;        // shared edge length (a)
        private const float HexSizeRatio = 180f;    // hexagon corner-to-corner (2a)
        private const float SquareSizeRatio = 90f;  // square edge
        private const float TriangleSizeRatio = 90f;

        // Editor-tunable rotation offsets in case the sprites' default facing
        // differs from the assumed flat-top / upward orientation.
        private const float SquareRotationOffset = 0f;
        private const float TriangleRotationOffset = 0f;

        // Distance/direction from the triangle sprite's pivot to its visual
        // centroid, expressed at rotation 0. It is rotated by each triangle's
        // own angle. (0, 13) reproduces the known-good (-13, 0) at 90 degrees.
        private static readonly Vector2 TriangleCentroidLocalOffset = new(0f, 13f);

        public override string ShapeId => "HexSquareTrianglePuzzle5";

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
            var colStep = a * (3f + Mathf.Sqrt(3f)) * 0.5f; // horizontal hex spacing
            var rowStep = a * (Mathf.Sqrt(3f) + 1f);        // vertical hex spacing

            // The upper-half neighbour directions of a flat-top hexagon centre, at
            // the nearest-neighbour distance (rowStep). Using only the upper half
            // enumerates every shared piece exactly once.
            var dirUp = new Vector2(0f, rowStep);                  // 90
            var dirUpRight = new Vector2(colStep, rowStep * 0.5f); // 30
            var dirUpLeft = new Vector2(-colStep, rowStep * 0.5f); // 150

            // 1) Build hexagon centres, column by column, centred per column so
            //    that columns whose heights differ by one interlock half a step.
            var columnHeights = BuildColumnHeights(rowCount, columnCount);
            var hexPositions = new List<Vector2>();
            var hexLookup = new Dictionary<long, int>();

            for (var column = 0; column < columnCount; column++)
            {
                var height = columnHeights[column];
                var x = (column - (columnCount - 1) * 0.5f) * colStep;

                for (var i = 0; i < height; i++)
                {
                    var y = (i - (height - 1) * 0.5f) * rowStep;
                    var position = new Vector2(x, y);
                    hexLookup[KeyFor(position)] = hexPositions.Count;
                    hexPositions.Add(position);
                }
            }

            // 2) Squares: one per shared edge between two existing hexagons.
            var squarePositions = new List<Vector2>();
            var squareRotations = new List<float>();
            // 3) Triangles: one per gap surrounded by three mutually adjacent hexagons.
            var trianglePositions = new List<Vector2>();
            var triangleRotations = new List<float>();
            var seenTriangles = new HashSet<long>();

            for (var index = 0; index < hexPositions.Count; index++)
            {
                var centre = hexPositions[index];

                var upPos = centre + dirUp;
                var upRightPos = centre + dirUpRight;
                var upLeftPos = centre + dirUpLeft;

                var hasUp = HexExists(hexLookup, upPos);
                var hasUpRight = HexExists(hexLookup, upRightPos);
                var hasUpLeft = HexExists(hexLookup, upLeftPos);

                // Vertical-neighbour square (0 degrees).
                if (hasUp)
                {
                    squarePositions.Add((centre + upPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(SquareRotationOffset, 90f));
                }

                // Up-right diagonal square (-> 30 degrees).
                if (hasUpRight)
                {
                    squarePositions.Add((centre + upRightPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(30f - 90f + SquareRotationOffset, 90f));
                }

                // Up-left diagonal square (-> 60 degrees).
                if (hasUpLeft)
                {
                    squarePositions.Add((centre + upLeftPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(150f - 90f + SquareRotationOffset, 90f));
                }

                // Triangle pointing left, between this hex and its up / up-right neighbours.
                if (hasUp && hasUpRight)
                {
                    AddTriangle(centre, upPos, upRightPos, -90f + TriangleRotationOffset,
                        trianglePositions, triangleRotations, seenTriangles);
                }

                // Triangle pointing right, between this hex and its up / up-left neighbours.
                if (hasUp && hasUpLeft)
                {
                    AddTriangle(centre, upPos, upLeftPos, -30f + TriangleRotationOffset,
                        trianglePositions, triangleRotations, seenTriangles);
                }
            }

            // 4) Pack groups in order: hexagons, squares, triangles.
            var positions = new List<Vector2>();
            var rotations = new List<float>();
            var pieceTypes = new List<int>();
            var sizeRatios = new List<Vector2>();

            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                hexPositions, null, HexagonPiece, new Vector2(HexSizeRatio, HexSizeRatio));
            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                squarePositions, squareRotations, SquarePiece, new Vector2(SquareSizeRatio, SquareSizeRatio));
            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                trianglePositions, triangleRotations, TrianglePiece, new Vector2(TriangleSizeRatio, TriangleSizeRatio));

            // 5) Centre on the board and scale to fit.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < positions.Count; index++)
            {
                min = Vector2.Min(min, positions[index]);
                max = Vector2.Max(max, positions[index]);
            }

            var contentWidth = (max.x - min.x) + HexSizeRatio;
            var contentHeight = (max.y - min.y) + HexSizeRatio;
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

            var hexCellSize = new Vector2(HexSizeRatio, HexSizeRatio) * scale;
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
            Vector2 a, Vector2 b, Vector2 c, float rotation,
            List<Vector2> positions, List<float> rotations, HashSet<long> seen)
        {
            var centroid = (a + b + c) / 3f;
            if (!seen.Add(KeyFor(centroid)))
            {
                return;
            }

            var normalizedRotation = NormalizeAngle(rotation, 120f);
            // The triangle sprite's visual centroid is offset from its pivot. That
            // offset rotates together with the piece, so rotate the local base
            // offset by the triangle's own rotation instead of using a fixed vector.
            var offset = RotateVector(TriangleCentroidLocalOffset, normalizedRotation);
            positions.Add(centroid + offset);
            rotations.Add(normalizedRotation);
        }

        private static Vector2 RotateVector(Vector2 vector, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
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

        private static bool HexExists(Dictionary<long, int> lookup, Vector2 position)
        {
            return lookup.ContainsKey(KeyFor(position));
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
