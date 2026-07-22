using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    public abstract class PuzzleBoardLayout
    {
        public abstract string ShapeId { get; }

        public abstract BoardLayoutResult BuildLayout(Rect boardRect, int rowCount, int columnCount);

        public virtual IReadOnlyList<int> BuildLockedSlots(BoardLayoutResult layoutResult, int rowCount, int columnCount)
        {
            if (layoutResult == null)
            {
                return new List<int>();
            }

            return GetConvexHullBoundarySlotIndices(layoutResult.SlotPositions);
        }

        protected static List<int> GetConvexHullBoundarySlotIndices(IReadOnlyList<Vector2> positions)
        {
            var boundarySlots = new List<int>();
            if (positions == null || positions.Count == 0)
            {
                return boundarySlots;
            }

            if (positions.Count <= 2)
            {
                for (var index = 0; index < positions.Count; index++)
                {
                    boundarySlots.Add(index);
                }

                return boundarySlots;
            }

            var hull = BuildConvexHull(positions);
            if (hull.Count <= 2)
            {
                for (var index = 0; index < positions.Count; index++)
                {
                    boundarySlots.Add(index);
                }

                return boundarySlots;
            }

            for (var index = 0; index < positions.Count; index++)
            {
                var point = positions[index];
                for (var hullIndex = 0; hullIndex < hull.Count; hullIndex++)
                {
                    var start = hull[hullIndex].Position;
                    var end = hull[(hullIndex + 1) % hull.Count].Position;
                    if (!PointLiesOnSegment(point, start, end))
                    {
                        continue;
                    }

                    boundarySlots.Add(index);
                    break;
                }
            }

            boundarySlots.Sort();
            return boundarySlots;
        }

        protected static List<int> GetAngularGapBoundarySlotIndices(
            IReadOnlyList<Vector2> positions,
            float neighbourRadiusMultiplier = 1.8f,
            float boundaryGapThresholdDegrees = 140f)
        {
            var boundarySlots = new List<int>();
            if (positions == null || positions.Count == 0)
            {
                return boundarySlots;
            }

            if (positions.Count <= 2)
            {
                for (var index = 0; index < positions.Count; index++)
                {
                    boundarySlots.Add(index);
                }

                return boundarySlots;
            }

            var nearestDistances = new List<float>(positions.Count);
            for (var index = 0; index < positions.Count; index++)
            {
                var nearestDistance = float.MaxValue;
                var source = positions[index];
                for (var otherIndex = 0; otherIndex < positions.Count; otherIndex++)
                {
                    if (otherIndex == index)
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(source, positions[otherIndex]);
                    if (distance > Mathf.Epsilon && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                    }
                }

                if (nearestDistance < float.MaxValue)
                {
                    nearestDistances.Add(nearestDistance);
                }
            }

            if (nearestDistances.Count == 0)
            {
                for (var index = 0; index < positions.Count; index++)
                {
                    boundarySlots.Add(index);
                }

                return boundarySlots;
            }

            nearestDistances.Sort();
            var medianNearestDistance = nearestDistances[nearestDistances.Count / 2];
            var neighbourRadius = medianNearestDistance * neighbourRadiusMultiplier;
            var angles = new List<float>(16);

            for (var index = 0; index < positions.Count; index++)
            {
                angles.Clear();
                var source = positions[index];

                for (var otherIndex = 0; otherIndex < positions.Count; otherIndex++)
                {
                    if (otherIndex == index)
                    {
                        continue;
                    }

                    var direction = positions[otherIndex] - source;
                    var distance = direction.magnitude;
                    if (distance <= Mathf.Epsilon || distance > neighbourRadius)
                    {
                        continue;
                    }

                    angles.Add(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }

                if (angles.Count < 3)
                {
                    boundarySlots.Add(index);
                    continue;
                }

                angles.Sort();
                var maxGap = 0f;
                for (var angleIndex = 0; angleIndex < angles.Count; angleIndex++)
                {
                    var current = angles[angleIndex];
                    var next = angles[(angleIndex + 1) % angles.Count];
                    var gap = next - current;
                    if (gap < 0f)
                    {
                        gap += 360f;
                    }

                    if (angleIndex == angles.Count - 1)
                    {
                        gap = (angles[0] + 360f) - current;
                    }

                    if (gap > maxGap)
                    {
                        maxGap = gap;
                    }
                }

                if (maxGap >= boundaryGapThresholdDegrees)
                {
                    boundarySlots.Add(index);
                }
            }

            boundarySlots.Sort();
            return boundarySlots;
        }

        private static List<IndexedPoint> BuildConvexHull(IReadOnlyList<Vector2> positions)
        {
            var points = new List<IndexedPoint>(positions.Count);
            for (var index = 0; index < positions.Count; index++)
            {
                points.Add(new IndexedPoint(positions[index]));
            }

            points.Sort((leftPoint, rightPoint) =>
            {
                var xCompare = leftPoint.Position.x.CompareTo(rightPoint.Position.x);
                return xCompare != 0 ? xCompare : leftPoint.Position.y.CompareTo(rightPoint.Position.y);
            });

            var lower = new List<IndexedPoint>();
            for (var index = 0; index < points.Count; index++)
            {
                while (lower.Count >= 2 && Cross(lower[^2].Position, lower[^1].Position, points[index].Position) <= Mathf.Epsilon)
                {
                    lower.RemoveAt(lower.Count - 1);
                }

                lower.Add(points[index]);
            }

            var upper = new List<IndexedPoint>();
            for (var index = points.Count - 1; index >= 0; index--)
            {
                while (upper.Count >= 2 && Cross(upper[^2].Position, upper[^1].Position, points[index].Position) <= Mathf.Epsilon)
                {
                    upper.RemoveAt(upper.Count - 1);
                }

                upper.Add(points[index]);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        private static bool PointLiesOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var segmentLength = segment.magnitude;
            if (segmentLength <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start) <= 0.01f;
            }

            var distanceFromLine = Mathf.Abs(Cross(start, end, point)) / segmentLength;
            if (distanceFromLine > 0.01f)
            {
                return false;
            }

            var dot = Vector2.Dot(point - start, segment);
            return dot >= -0.01f && dot <= segment.sqrMagnitude + 0.01f;
        }

        private static float Cross(Vector2 origin, Vector2 first, Vector2 second)
        {
            return (first.x - origin.x) * (second.y - origin.y) - (first.y - origin.y) * (second.x - origin.x);
        }

        private readonly struct IndexedPoint
        {
            public IndexedPoint(Vector2 position)
            {
                Position = position;
            }

            public Vector2 Position { get; }
        }
    }
}
