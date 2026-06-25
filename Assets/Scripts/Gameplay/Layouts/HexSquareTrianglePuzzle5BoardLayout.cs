using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    public sealed class HexSquareTrianglePuzzle5BoardLayout : PuzzleBoardLayout
    {
        private const int HexagonPiece = 0;
        private const int SquarePiece = 1;
        private const int TrianglePiece = 2;

        private const float EdgeRatio = 90f;       
        private const float HexSizeRatio = 180f;    
        private const float SquareSizeRatio = 90f;  
        private const float TriangleSizeRatio = 90f;

        private const float SquareRotationOffset = 0f;
        private const float TriangleRotationOffset = 0f;

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
            var colStep = a * (3f + Mathf.Sqrt(3f)) * 0.5f; 
            var rowStep = a * (Mathf.Sqrt(3f) + 1f);        

            var dirUp = new Vector2(0f, rowStep);                  
            var dirUpRight = new Vector2(colStep, rowStep * 0.5f); 
            var dirUpLeft = new Vector2(-colStep, rowStep * 0.5f);

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

            var squarePositions = new List<Vector2>();
            var squareRotations = new List<float>();
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

                if (hasUp)
                {
                    squarePositions.Add((centre + upPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(SquareRotationOffset, 90f));
                }

                if (hasUpRight)
                {
                    squarePositions.Add((centre + upRightPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(30f - 90f + SquareRotationOffset, 90f));
                }

                if (hasUpLeft)
                {
                    squarePositions.Add((centre + upLeftPos) * 0.5f);
                    squareRotations.Add(NormalizeAngle(150f - 90f + SquareRotationOffset, 90f));
                }

                if (hasUp && hasUpRight)
                {
                    AddTriangle(centre, upPos, upRightPos, -90f + TriangleRotationOffset,
                        trianglePositions, triangleRotations, seenTriangles);
                }

                if (hasUp && hasUpLeft)
                {
                    AddTriangle(centre, upPos, upLeftPos, -30f + TriangleRotationOffset,
                        trianglePositions, triangleRotations, seenTriangles);
                }
            }

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
