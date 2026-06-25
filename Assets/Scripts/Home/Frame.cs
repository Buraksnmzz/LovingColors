using TMPro;
using UnityEngine;

namespace Home
{
    public class Frame : MonoBehaviour
    {
        [SerializeField] private GameObject completedImage;
        [SerializeField] private GameObject normalImage;
        [SerializeField] private GameObject hardImage;
        [SerializeField] private GameObject superHardImage;
        [SerializeField] private GameObject pipe;
        [SerializeField] private TextMeshProUGUI normalLevelText;
        [SerializeField] private TextMeshProUGUI completedLevelText;
        [SerializeField] private TextMeshProUGUI hardLevelText;
        [SerializeField] private TextMeshProUGUI superHardLevelText;

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void SetPipeVisible(bool isVisible)
        {
            pipe.SetActive(isVisible);
        }

        public void SetLevel(int levelNumber, LevelDifficultyType levelDifficultyType, bool isCompleted)
        {
            var levelText = levelNumber.ToString();
            normalLevelText.text = levelText;
            completedLevelText.text = levelText;
            hardLevelText.text = levelText;
            superHardLevelText.text = levelText;

            completedImage.SetActive(isCompleted);
            if (isCompleted)
            {
                normalImage.SetActive(false);
                hardImage.SetActive(false);
                superHardImage.SetActive(false);
                return;
            }

            normalImage.SetActive(levelDifficultyType == LevelDifficultyType.Normal);
            hardImage.SetActive(levelDifficultyType == LevelDifficultyType.Hard);
            superHardImage.SetActive(levelDifficultyType == LevelDifficultyType.SuperHard);
        }
    }
}