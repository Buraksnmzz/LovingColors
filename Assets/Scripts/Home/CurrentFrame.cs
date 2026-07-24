using TMPro;
using UnityEngine;

namespace Home
{
    public class CurrentFrame : MonoBehaviour
    {
        [SerializeField] private GameObject normalImage;
        [SerializeField] private GameObject hardImage;
        [SerializeField] private GameObject superHardImage;
        [SerializeField] private GameObject pipe;

        public void SetPipeVisible(bool isVisible)
        {
            pipe.SetActive(isVisible);
        }

        public void SetLevel(int levelNumber, LevelDifficultyType levelDifficultyType)
        {
            normalImage.SetActive(levelDifficultyType == LevelDifficultyType.Normal);
            hardImage.SetActive(levelDifficultyType == LevelDifficultyType.Hard);
            superHardImage.SetActive(levelDifficultyType == LevelDifficultyType.Extreme);
        }
    }
}