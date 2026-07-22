using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetHint
{
    public class GetHintView : BaseView
    {
        [SerializeField] private Button getHintWithCoinButton;
        [SerializeField] private Button getHintWithVideoButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI hintAmountText;
        [SerializeField] private TextMeshProUGUI coinAmountText;
        [SerializeField] private TextMeshProUGUI hintCostText;
        [SerializeField] private Transform topBar;
        [SerializeField] private Transform hintImage;
        [SerializeField] private Transform hintImageTarget;
        [SerializeField] private float topBarIntroDuration = 0.35f;
        [SerializeField] private float topBarIntroYOffset = 260f;
        [SerializeField] private float hintFlyDuration = 0.5f;

        public event Action GetHintWithCoinButtonClicked;
        public event Action GetHintWithVideoButtonClicked;

        private Tween _topBarTween;
        private Tween _hintFlyTween;
        private Vector3 _topBarInitialLocalPosition;
        private bool _hasTopBarInitialLocalPosition;
        private Vector3 _hintImageInitialPosition;
        private bool _hasHintImageInitialPosition;

        private void Start()
        {
            CacheTopBarPosition();
            CacheHintImagePosition();
            if (hintImage != null)
                hintImage.gameObject.SetActive(false);

            closeButton.onClick.AddListener(Hide);
            getHintWithCoinButton.onClick.AddListener(() => GetHintWithCoinButtonClicked?.Invoke());
            getHintWithVideoButton.onClick.AddListener(() => GetHintWithVideoButtonClicked?.Invoke());
        }

        public void SetHintCostText(int amount)
        {
            hintCostText.text = amount.ToString();
        }

        public override void Show()
        {
            PrepareTopBarIntro();
            base.Show();
        }

        protected override void OnShown()
        {
            base.OnShown();
            PlayTopBarIntro();
        }

        public override void Hide()
        {
            base.Hide();
            PlayTopBarClose();
        }

        protected override void OnHidden()
        {
            base.OnHidden();
        }

        protected override void OnDestroy()
        {
            _topBarTween?.Kill();
            _hintFlyTween?.Kill();
            base.OnDestroy();
        }

        public void SetHintAmount(int value)
        {
            if (hintAmountText != null)
                hintAmountText.text = value.ToString();
        }

        public void SetCoinAmount(int value)
        {
            if (coinAmountText != null)
                coinAmountText.text = value.ToString();
        }

        public void SetButtonsInteractable(bool isInteractable)
        {
            if (getHintWithCoinButton != null)
                getHintWithCoinButton.interactable = isInteractable;
            if (getHintWithVideoButton != null)
                getHintWithVideoButton.interactable = isInteractable;
        }

        public void PlayHintFly(Action onCompleted)
        {
            if (hintImage == null || hintImageTarget == null)
            {
                onCompleted?.Invoke();
                return;
            }

            CacheHintImagePosition();
            _hintFlyTween?.Kill();
            hintImage.gameObject.SetActive(true);
            hintImage.position = _hintImageInitialPosition;

            _hintFlyTween = hintImage.DOMove(hintImageTarget.position, hintFlyDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    hintImage.gameObject.SetActive(false);
                    hintImage.position = _hintImageInitialPosition;
                    onCompleted?.Invoke();
                });
        }

        private void CacheTopBarPosition()
        {
            if (_hasTopBarInitialLocalPosition || topBar == null)
                return;

            _topBarInitialLocalPosition = topBar.localPosition;
            _hasTopBarInitialLocalPosition = true;
        }

        private void CacheHintImagePosition()
        {
            if (_hasHintImageInitialPosition || hintImage == null)
                return;

            _hintImageInitialPosition = hintImage.position;
            _hasHintImageInitialPosition = true;
        }

        private void PrepareTopBarIntro()
        {
            if (topBar == null)
                return;

            CacheTopBarPosition();
            topBar.localPosition = _topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset;
        }

        private void PlayTopBarIntro()
        {
            if (topBar == null)
                return;

            CacheTopBarPosition();
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition, topBarIntroDuration)
                .SetEase(Ease.OutBack);
        }
        
        private void PlayTopBarClose()
        {
            CacheTopBarPosition();
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset, topBarIntroDuration)
                .SetEase(Ease.InBack);
        }
    }
}