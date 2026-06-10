using System.Collections.Generic;
using DefaultNamespace;

namespace Gameplay.Levels
{
    public class LockOneByOne : LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    if ((row + col) % 2 == 0)
                    {
                        cards[row * columnCount + col].IsLocked = true;
                    }
                }
            }

            base.LockCards(cards, columnCount, rowCount);
        }
    }
}