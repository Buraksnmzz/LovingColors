using System.Collections.Generic;
using DefaultNamespace;

namespace Gameplay.Levels
{
    public class LockInPairsAndUnlockLines : LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int row = 0; row < rowCount; row++)
            {
                if (row % 2 == 0)
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        cards[row * columnCount + col].IsLocked = col % 2 == 0;
                    }
                }
                else
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        cards[row * columnCount + col].IsLocked = false;
                    }
                }
            }
            base.LockCards(cards, columnCount, rowCount);
        }
    }
}