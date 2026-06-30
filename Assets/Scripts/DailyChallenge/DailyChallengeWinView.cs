using System;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeWinView : BaseView
    {
        public event Action ContinueClicked;

        [SerializeField] private Button continueButton;

        private void Start()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        protected override void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }

            base.OnDestroy();
        }

        private void OnContinueButtonClicked()
        {
            ContinueClicked?.Invoke();
        }
    }
}