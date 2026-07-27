using System;
using UnityEngine;
using UnityEngine.UI;

namespace SuperPinOffer
{
    public class SuperPinOfferView : BaseView
    {
        [SerializeField] Button pinButton;
        [SerializeField] Button noButton;

        public event Action PinButtonClicked;
        public event Action NoButtonClicked;

        private void Start()
        {
            pinButton.onClick.AddListener(OnPinButtonClick);
            noButton.onClick.AddListener(OnNoButtonClick);
        }

        protected override void OnDestroy()
        {
            pinButton.onClick.RemoveListener(OnPinButtonClick);
            noButton.onClick.RemoveListener(OnNoButtonClick);
            base.OnDestroy();
        }

        private void OnPinButtonClick()
        {
            PinButtonClicked?.Invoke();
        }

        private void OnNoButtonClick()
        {
            NoButtonClicked?.Invoke();
        }

        public void SetButtonsInteractable(bool isInteractable)
        {
            if (pinButton != null)
            {
                pinButton.interactable = isInteractable;
            }

            if (noButton != null)
            {
                noButton.interactable = isInteractable;
            }
        }
    }
}