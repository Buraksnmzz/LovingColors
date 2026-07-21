using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    /// <summary>
    /// "starTrianglePuzzle9" shape: pointy-top hexagram stars arranged on a
    /// triangular lattice (row r holds r+1 stars), so rowCount = 4 gives
    /// 1+2+3+4 = 10 stars stacked apex-up.
    ///
    /// The stars (pieceType 0) sit at the hexagon centres. The gaps between every
    /// two ADJACENT stars are filled by a single deltoid (pieceType 1); there are
    /// no outer triangles. Each deltoid is the rhombus formed where two adjacent
    /// hexagrams meet, centred on the shared hexagon edge.
    /// So rowCount = 4 produces 10 stars + 18 deltoids = 28 slots.
    ///
    /// Geometry (hexagon circumradius R, pointy-top so a vertex points up):
    ///   - The hexagram's six outer points coincide with the hexagon's six
    ///     vertices (radius R, angles 90,150,...,30). Its bounding box equals the
    ///     hexagon's: width = R*sqrt(3), height = 2R.
    ///   - Adjacent star centres are R*sqrt(3) apart. The deltoid between them is a
    ///     rhombus whose long diagonal (= R) lies along the shared hexagon edge and
    ///     whose short diagonal (= R/sqrt(3)) lies along the line joining the two
    ///     centres. It is centred at the midpoint of the two centres.
    ///
    /// The deltoid sprite is authored upright with its long axis vertical
    /// (bounding box width = R/sqrt(3), height = R) at rotation 0. Each deltoid is
    /// rotated so its long axis aligns with the shared hexagon edge. Deltoid
    /// rotations use the three canonical values -60, 0 and 60 degrees; 120 is
    /// visually equivalent to -60 due to the rhombus' 180-degree symmetry. The
    /// star sprite is authored pointy-top and is not rotated.
    ///
    /// Slot index order (predictable for lockedSlots): all stars first (in lattice
    /// order), then all deltoids (in adjacent-pair discovery order).
    ///
    /// Keep prefab sprite rotation at 0 so the layout fully controls each slot.
    /// </summary>
    public sealed class HexStarTrianglePuzzle9BoardLayout : PuzzleBoardLayout
    {
        private const int StarPiece = 0;
        private const int DeltoidPiece = 1;

        private const float Radius = 100f; // hexagon circumradius (R)

        // Hexagram star bounding box (pointy-top): width = R*sqrt(3), height = 2R.
        private static readonly float StarWidthRatio = Radius * Mathf.Sqrt(3f);
        private const float StarHeightRatio = 2f * Radius;

        // Deltoid (inter-star rhombus) bounding box, authored long-axis vertical:
        // width = short diagonal = R/sqrt(3), height = long diagonal = R.
        private static readonly float DeltoidWidthRatio = Radius / Mathf.Sqrt(3f);
        private const float DeltoidHeightRatio = Radius;

        // Adjacent star centres sit exactly this far apart on the lattice.
        private static readonly float NeighbourDistance = Radius * Mathf.Sqrt(3f);

        public override string ShapeId => "starTrianglePuzzle9";

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
            var horizontalStep = r * Mathf.Sqrt(3f); // pointy-top hex width
            var verticalStep = r * 1.5f;             // pointy-top row spacing (3/4 of height)

            // 1) Hexagon centres in a triangular formation (row k holds k+1 hexes).
            var hexCentres = new List<Vector2>();
            var midRow = (rowCount - 1) * 0.5f;
            for (var row = 0; row < rowCount; row++)
            {
                var y = -(row - midRow) * verticalStep; // apex row on top
                for (var i = 0; i <= row; i++)
                {
                    var x = (i - row * 0.5f) * horizontalStep;
                    hexCentres.Add(new Vector2(x, y));
                }
            }

            // 2) One deltoid between every pair of adjacent stars.
            var starPositions = new List<Vector2>(hexCentres.Count);
            foreach (var centre in hexCentres)
            {
                starPositions.Add(centre);
            }

            var deltoidPositions = new List<Vector2>();
            var deltoidRotations = new List<float>();
            var toleranceSqr = (NeighbourDistance * 0.1f) * (NeighbourDistance * 0.1f);

            for (var a = 0; a < hexCentres.Count; a++)
            {
                for (var b = a + 1; b < hexCentres.Count; b++)
                {
                    var delta = hexCentres[b] - hexCentres[a];
                    var distanceError = delta.magnitude - NeighbourDistance;
                    if (distanceError * distanceError > toleranceSqr)
                    {
                        continue;
                    }

                    deltoidPositions.Add((hexCentres[a] + hexCentres[b]) * 0.5f);
                    // Sprite long axis is authored vertical (90 deg); align it with the
                    // shared hexagon edge, which is perpendicular to the centre-to-centre
                    // direction. That works out to a rotation equal to the centre-to-
                    // centre angle itself.
                    var centreAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    deltoidRotations.Add(GetCanonicalDeltoidRotation(centreAngle));
                }
            }

            // 3) Pack groups in order: stars, then deltoids.
            var positions = new List<Vector2>();
            var rotations = new List<float>();
            var pieceTypes = new List<int>();
            var sizeRatios = new List<Vector2>();

            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                starPositions, null, StarPiece, new Vector2(StarWidthRatio, StarHeightRatio));
            AppendGroup(positions, rotations, pieceTypes, sizeRatios,
                deltoidPositions, deltoidRotations, DeltoidPiece, new Vector2(DeltoidWidthRatio, DeltoidHeightRatio));

            // 4) Centre on the board and scale to fit.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var position in positions)
            {
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var contentWidth = (max.x - min.x) + StarWidthRatio;
            var contentHeight = (max.y - min.y) + StarHeightRatio;
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

            var cellSize = new Vector2(StarWidthRatio, StarHeightRatio) * scale;
            return new BoardLayoutResult(finalPositions, cellSize, rotations, pieceTypes)
            {
                SlotSizes = finalSizes
            };
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

        private static float NormalizeAngle(float angle, float period)
        {
            var result = angle % period;
            if (result < 0f)
            {
                result += period;
            }

            return result;
        }

        private static float GetCanonicalDeltoidRotation(float centreAngle)
        {
            var rotation = NormalizeAngle(centreAngle, 180f);
            return rotation > 90f ? rotation - 180f : rotation;
        }
    }
}
