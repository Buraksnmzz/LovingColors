using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "LockRules/LockEdgesAndWindow")] 
    public class LockEdgesAndWindow: LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            int middleColumn = columnCount / 2;
            int middleRow = rowCount / 2;

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    if (row == 0 || row == rowCount - 1 || col == 0 || col == columnCount - 1 || 
                        row == middleRow || col == middleColumn)
                    {
                        cards[row * columnCount + col].IsLocked = true;
                    }
                    else
                    {
                        cards[row * columnCount + col].IsLocked = false;
                    }
                }
            }
        }
    }
}