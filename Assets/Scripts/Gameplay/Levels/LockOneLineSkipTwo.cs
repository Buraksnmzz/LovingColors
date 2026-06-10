using System.Collections.Generic;
using DefaultNamespace;

namespace Gameplay.Levels
{
    public class LockOneLineSkipTwo : LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int row = 0; row < rowCount; row++)
            {
                bool isLockedRow = row % 3 == 0;
                for (int col = 0; col < columnCount; col++)
                {
                    cards[row * columnCount + col].IsLocked = isLockedRow;
                }
            }
            base.LockCards(cards, columnCount, rowCount);
        }
    }
}