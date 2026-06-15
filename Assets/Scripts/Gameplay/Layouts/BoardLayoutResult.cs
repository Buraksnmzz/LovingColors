using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Layouts
{
    public sealed class BoardLayoutResult
    {
        public BoardLayoutResult(List<Vector2> slotPositions, Vector2 cellSize)
            : this(slotPositions, cellSize, null)
        {
        }

        public BoardLayoutResult(List<Vector2> slotPositions, Vector2 cellSize, List<float> slotRotations)
            : this(slotPositions, cellSize, slotRotations, null)
        {
        }

        public BoardLayoutResult(List<Vector2> slotPositions, Vector2 cellSize, List<float> slotRotations, List<int> slotPieceTypes)
        {
            SlotPositions = slotPositions;
            CellSize = cellSize;
            SlotRotations = slotRotations;
            SlotPieceTypes = slotPieceTypes;
        }

        public List<Vector2> SlotPositions { get; }
        public Vector2 CellSize { get; }
        public List<float> SlotRotations { get; }
        public List<int> SlotPieceTypes { get; }
        public List<Vector2> SlotSizes { get; set; }
        public int SlotCount => SlotPositions.Count;
    }
}
