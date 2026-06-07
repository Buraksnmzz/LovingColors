using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "LockRules/LockOneRowSkipOne")] 
    public class LockOneRowSkipOne: LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            for (int row = 0; row < rowCount; row += 2)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    cards[row * columnCount + col].IsLocked = true;
                }
            }
            
            base.LockCards(cards, columnCount, rowCount);
        }
    }
}