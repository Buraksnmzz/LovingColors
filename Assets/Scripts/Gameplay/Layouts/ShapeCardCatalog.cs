using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Layouts
{
    [CreateAssetMenu(fileName = "ShapeCardCatalog", menuName = "LovingColors/Shape Card Catalog")]
    public sealed class ShapeCardCatalog : ScriptableObject
    {
        [Serializable]
        private struct ShapeCardEntry
        {
            public string shapeId;

            [Tooltip("One prefab per piece type. Index 0 is the default piece. " +
                     "For shapes with multiple piece types (e.g. wavePuzzle: 0=square, 1=deltoid) add more.")]
            public Card[] piecePrefabs;
        }

        [SerializeField] private List<ShapeCardEntry> entries = new();

        public Card ResolvePrefab(string shapeId, int pieceType)
        {
            if (string.IsNullOrWhiteSpace(shapeId))
            {
                return null;
            }

            for (var index = 0; index < entries.Count; index++)
            {
                if (!string.Equals(entries[index].shapeId, shapeId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var prefabs = entries[index].piecePrefabs;
                if (prefabs == null || prefabs.Length == 0)
                {
                    return null;
                }

                if (pieceType < 0 || pieceType >= prefabs.Length)
                {
                    return prefabs[0];
                }

                return prefabs[pieceType];
            }

            return null;
        }
    }
}
