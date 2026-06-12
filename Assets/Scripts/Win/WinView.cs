using System;
using UnityEngine;
using UnityEngine.UI;

namespace Win
{
    public class WinView : BaseView
    {
        [SerializeField] private Button nextButton;

        public event Action NextButtonClicked;

        private void Start()
        {
            nextButton.onClick.AddListener(OnNextButtonClick);
        }

        protected override void OnDestroy()
        {
            nextButton.onClick.RemoveListener(OnNextButtonClick);
            base.OnDestroy();
        }

        private void OnNextButtonClick()
        {
            NextButtonClicked?.Invoke();
        }

    }
}