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
                var lockedSlots = layout.BuildLockedSlots(layoutResult, rowCount, columnCount);
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
    }
}