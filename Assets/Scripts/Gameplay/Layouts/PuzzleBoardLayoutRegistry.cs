using System;
using System.Collections.Generic;

namespace Gameplay.Layouts
{
    public static class PuzzleBoardLayoutRegistry
    {
        private static readonly Dictionary<string, PuzzleBoardLayout> LayoutsByShapeId = new(StringComparer.OrdinalIgnoreCase)
        {
            { "tiePuzzle2", new TiePuzzle2BoardLayout() },
            { "puzzle3", new HexPuzzleBoardLayout() },
            { "wavePuzzle3", new WavePuzzleBoardLayout() },
            { "OctaSquarePuzzle4", new OctaSquarePuzzle4BoardLayout() },
            { "hexSquareTrianglePuzzle5", new HexSquareTrianglePuzzle5BoardLayout() }
        };

        public static bool TryGet(string shapeId, out PuzzleBoardLayout layout)
        {
            if (string.IsNullOrWhiteSpace(shapeId))
            {
                layout = null;
                return false;
            }

            return LayoutsByShapeId.TryGetValue(shapeId, out layout);
        }
    }
}
