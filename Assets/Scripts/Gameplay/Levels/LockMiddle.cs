using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "LockRules/LockMiddle")] 
    public class LockMiddle:LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    if (row == 0 || row == rowCount - 1 || col == 0 || col == columnCount - 1)
                    {
                        cards[row * columnCount + col].IsLocked = false;
                    }
                    else
                    {
                        cards[row * columnCount + col].IsLocked = true;
                    }
                }
            }
            base.LockCards(cards, columnCount, rowCount);
        }
    }
}