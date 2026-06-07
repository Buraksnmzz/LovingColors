using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class Board: MonoBehaviour
    {
        private int _rowCount;
        private int _columnCount;
        private Color _colorA;
        private Color _colorB;
        private Color _colorC;
        private Color _colorD;
        private List<Card> _cards;


        void CreateCards()
        {
            
        }
        
        void LockCards()
        {
            
        }
        
        void AssignColors()
        {
            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _columnCount; c++)
                {
                    float aContribution = Mathf.Clamp01(1f - (float)c / (_columnCount - 1)) * Mathf.Clamp01(1f - (float)r / (_rowCount - 1));
                    float bContribution = Mathf.Clamp01((float)c / (_columnCount - 1)) * Mathf.Clamp01(1f - (float)r / (_rowCount - 1));
                    float cContribution = Mathf.Clamp01(1f - (float)c / (_columnCount - 1)) * Mathf.Clamp01((float)r / (_rowCount - 1));
                    float dContribution = Mathf.Clamp01((float)c / (_columnCount - 1)) * Mathf.Clamp01((float)r / (_rowCount - 1));
                    Color finalColor = _colorA * aContribution + _colorB * bContribution + _colorC * cContribution + _colorD * dContribution;
                    _cards[r * _columnCount + c].GetComponent<Image>().color = finalColor;
                }
            }
        }
    }
}