using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Gameplay.Levels
{
    public abstract class LockRule: ScriptableObject
    {
        public virtual void LockCards(List<Card> cards, int columnCount, int rowCount)
        {
            cards[0].IsLocked = true;
            cards[columnCount - 1].IsLocked = true;
            cards[(rowCount - 1) * columnCount].IsLocked = true;
            cards[rowCount * columnCount - 1].IsLocked = true;
        }
    }
}