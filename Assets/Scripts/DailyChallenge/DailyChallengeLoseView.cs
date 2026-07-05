using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeLoseView : BaseView
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button addMovesButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI extraMovesCostText;
        [SerializeField] private TextMeshProUGUI youCanAddMovesText;
        [SerializeField] private TextMeshProUGUI plusXMovesText;

        public event Action RestartButtonClicked;
        public event Action AddMovesClicked;
        public event Action ContinueButtonClicked;

        private void Start()
        {
            restartButton.onClick.AddListener(() => RestartButtonClicked?.Invoke());
            addMovesButton.onClick.AddListener(() => AddMovesClicked?.Invoke());
            continueButton.onClick.AddListener(() => ContinueButtonClicked?.Invoke());
        }

        public void SetAddMovesButtonActive(bool isActive)
        {
            addMovesButton.gameObject.SetActive(isActive);
        }

        public void SetExtraMovesCostText(int extraMovesCost)
        {
            extraMovesCostText.text = extraMovesCost.ToString();
        }

        public void SetYouCanAddMovesText(string addMovesText)
        {
            youCanAddMovesText.text = addMovesText;
        }

        public void SetPlusXMovesText(string xMovesText)
        {
            plusXMovesText.text = xMovesText;
        }
 
    }
}
