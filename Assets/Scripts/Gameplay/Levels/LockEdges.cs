using System.Collections.Generic;
using DefaultNamespace;

namespace Gameplay.Levels
{
    public class LockEdges : LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int i = 0; i < columnCount; i++)
            {
                cards[i].IsLocked = true;
            }
            for (int i = (rowCount - 1) * columnCount; i < columnCount * rowCount; i++)
            {
                cards[i].IsLocked = true;
            }
            for (int i = 0; i < rowCount; i++)
            {
                cards[i * columnCount].IsLocked = true;
            }
            for (int i = 1; i <= rowCount; i++)
            {
                cards[i * columnCount - 1].IsLocked = true;
            }
        }
    }
}