using UnityEngine;

namespace Gameplay.Layouts
{
    public abstract class PuzzleBoardLayout
    {
        public abstract string ShapeId { get; }

        public abstract BoardLayoutResult BuildLayout(Rect boardRect, int rowCount, int columnCount);
    }
}
