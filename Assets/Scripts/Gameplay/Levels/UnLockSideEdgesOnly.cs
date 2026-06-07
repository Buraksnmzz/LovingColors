using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "LockRules/LockSideEdgesOnly")] 
    public class UnLockSideEdgesOnly : LockRule
    {
        public override void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            foreach (var card in cards)
            {
                card.IsLocked = true;
            }

            for (var i = 0; i < rowCount; i++)
            {
                cards[i * columnCount].IsLocked = false;
                cards[(i * columnCount) + (columnCount - 1)].IsLocked = false;
            }
            base.LockCards(cards, columnCount, rowCount);
        }
    }

}