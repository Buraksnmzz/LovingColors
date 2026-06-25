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
        [SerializeField] private TextMeshProUGUI normalLevelText;
        [SerializeField] private TextMeshProUGUI hardLevelText;
        [SerializeField] private TextMeshProUGUI superHardLevelText;

        public void SetPipeVisible(bool isVisible)
        {
            pipe.SetActive(isVisible);
        }

        public void SetLevel(int levelNumber, LevelDifficultyType levelDifficultyType)
        {
            var levelText = levelNumber.ToString();
            normalLevelText.text = levelText;
            hardLevelText.text = levelText;
            superHardLevelText.text = levelText;

            normalImage.SetActive(levelDifficultyType == LevelDifficultyType.Normal);
            hardImage.SetActive(levelDifficultyType == LevelDifficultyType.Hard);
            superHardImage.SetActive(levelDifficultyType == LevelDifficultyType.SuperHard);
        }
    }
}