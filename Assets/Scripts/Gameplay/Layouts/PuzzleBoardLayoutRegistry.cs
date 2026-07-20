using System;
using System.Collections.Generic;

namespace Gameplay.Layouts
{
    public static class PuzzleBoardLayoutRegistry
    {
        private static readonly Dictionary<string, PuzzleBoardLayout> LayoutsByShapeId = new(StringComparer.OrdinalIgnoreCase)
        {
            { "hexPuzzle1", new HexPuzzleBoardLayout() },
            { "tiePuzzle2", new TiePuzzle2BoardLayout() },
            { "wavePuzzle3", new WavePuzzle3BoardLayout() },
            { "OctaSquarePuzzle4", new OctaSquarePuzzle4BoardLayout() },
            { "hexSquareTrianglePuzzle5", new HexSquareTrianglePuzzle5BoardLayout() },
            { "hexTrianglePuzzle6", new HexTrianglePuzzle6BoardLayout() },
            { "octaSquarePuzzle7", new OctaSquarePuzzle7BoardLayout() },
            { "triHexPuzzle8", new TriHexPuzzle8BoardLayout() },
            { "starTrianglePuzzle9", new HexStarTrianglePuzzle9BoardLayout() },
            { "arrowPuzzle10", new ArrowPuzzle10BoardLayout() }
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
