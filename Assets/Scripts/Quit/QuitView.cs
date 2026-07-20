using System;
using UnityEngine;
using UnityEngine.UI;

namespace Quit
{
    public class QuitView : BaseView
    {
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button closeButton;

        public Action YesButtonClicked;

        private void Start()
        {
            yesButton.onClick.AddListener(() => YesButtonClicked?.Invoke());
            noButton.onClick.AddListener(OnCancelClicked);
            closeButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnCancelClicked()
        {
            Hide();
        }
    }
}