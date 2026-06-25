using System;
using System.Collections.Generic;
using System.IO;
using Gameplay.Layouts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace LovingColors.Editor
{
    public sealed class CustomShapeLevelLockUtility : EditorWindow
    {
        private const string LevelJsonAssetPath = "Assets/Scripts/Gameplay/Levels/lovingColorsLevels.json";
        private static readonly Rect LayoutProbeRect = new Rect(0f, 0f, 10000f, 10000f);

        [MenuItem("Tools/Loving Colors/Levels/Custom Shape Lock Utility")]
        private static void OpenWindow()
        {
            var window = GetWindow<CustomShapeLevelLockUtility>("Shape Locks");
            window.minSize = new Vector2(360f, 96f);
        }

        [MenuItem("Tools/Loving Colors/Levels/Regenerate Custom Shape Locked Slots")]
        private static void RegenerateFromMenu()
        {
            RegenerateLockedSlots(LevelJsonAssetPath, true);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Level JSON", LevelJsonAssetPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Regenerate Custom Shape Locked Slots", GUILayout.Height(32f)))
            {
                RegenerateLockedSlots(LevelJsonAssetPath, true);
            }
        }

        private static void RegenerateLockedSlots(string assetPath, bool showDialog)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Level json not found at '{assetPath}'.");
                return;
            }

            JArray levels;
            try
            {
                levels = JArray.Parse(File.ReadAllText(fullPath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Level json could not be parsed: {exception.Message}");
                return;
            }

            var updatedCount = 0;
            var skippedCount = 0;

            foreach (var token in levels)
            {
                if (token is not JObject levelObject)
                {
                    skippedCount++;
                    continue;
                }

                var shapeId = levelObject.Value<string>("shapeId");
                if (string.IsNullOrWhiteSpace(shapeId))
                {
                    levelObject.Property("lockedSlots")?.Remove();
                    continue;
                }

                if (!PuzzleBoardLayoutRegistry.TryGet(shapeId, out var layout))
                {
                    levelObject.Property("lockedSlots")?.Remove();
                    skippedCount++;
                    Debug.LogWarning($"No custom layout registered for shapeId '{shapeId}'.");
                    continue;
                }

                var rowCount = levelObject.Value<int?>("rowCount") ?? 0;
                var columnCount = levelObject.Value<int?>("columnCount") ?? 0;
                if (rowCount <= 0 || columnCount <= 0)
                {
                    skippedCount++;
                    Debug.LogWarning($"Level '{levelObject.Value<int?>("levelId")}' has invalid row/column count.");
                    continue;
                }

                var layoutResult = layout.BuildLayout(LayoutProbeRect, rowCount, columnCount);
                var lockedSlots = GetBoundarySlotIndices(layoutResult.SlotPositions);
                SetLockedSlotsAfterShapeId(levelObject, lockedSlots);
                updatedCount++;
            }

            File.WriteAllText(fullPath, levels.ToString(Formatting.Indented));
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();

            var message = $"Updated lockedSlots for {updatedCount} custom-shaped levels.";
            if (skippedCount > 0)
            {
                message += $" Skipped {skippedCount} invalid or unknown entries.";
            }

            Debug.Log(message);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Custom Shape Locks", message, "OK");
            }
        }

        private static void SetLockedSlotsAfterShapeId(JObject levelObject, IReadOnlyList<int> lockedSlots)
        {
            var lockedSlotsArray = new JArray();
            for (var index = 0; index < lockedSlots.Count; index++)
            {
                lockedSlotsArray.Add(lockedSlots[index]);
            }

            var existingProperty = levelObject.Property("lockedSlots");
            existingProperty?.Remove();

            var shapeIdProperty = levelObject.Property("shapeId");
            if (shapeIdProperty != null)
            {
                shapeIdProperty.AddAfterSelf(new JProperty("lockedSlots", lockedSlotsArray));
                return;
            }

            levelObject.Add("lockedSlots", lockedSlotsArray);
        }

        private static List<int> GetBoundarySlotIndices(IReadOnlyList<Vector2> positions)
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