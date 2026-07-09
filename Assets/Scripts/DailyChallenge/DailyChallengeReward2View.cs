using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UI.General;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeReward2View : BaseView
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI coinWonAmountText;
        [SerializeField] private Image coinImage;
        [SerializeField] private Button claimDouble;
        [SerializeField] private Button claim;
        [SerializeField] private CoinFlyAnimator coinFlyAnimator;
        [SerializeField] private float stepDelay = 0.2f;
        [SerializeField] private float durations = 0.35f;

        public event Action ClaimClicked;
        public event Action ClaimDoubleClicked;

        private Sequence _animationSequence;
        private CoinFlyAnimator _coinFlyAnimatorInstance;

        private void Start()
        {
            EnsureCoinFlyAnimatorInstance();
            claim.onClick.AddListener(() => ClaimClicked?.Invoke());
            claimDouble.onClick.AddListener(() => ClaimDoubleClicked?.Invoke());
        }

        public void SetCoinCount(int value)
        {
            var animator = EnsureCoinFlyAnimatorInstance();
            if (animator != null)
                animator.SetCoinCount(value);
        }

        public void SetCoinWonAmount(int value)
        {
            if (coinWonAmountText != null)
                coinWonAmountText.text = value.ToString();
        }

        public void SetButtonsInteractable(bool isInteractable)
        {
            if (claim != null)
                claim.interactable = isInteractable;
            if (claimDouble != null)
                claimDouble.interactable = isInteractable;
        }

        public void CompleteCoinAnimation()
        {
            if (_coinFlyAnimatorInstance != null)
                _coinFlyAnimatorInstance.Complete();
        }

        public void PlayCoinAnimation(int finalCoinCount, Action onCompleted)
        {
            var animator = EnsureCoinFlyAnimatorInstance();
            if (animator == null || coinImage == null)
            {
                onCompleted?.Invoke();
                return;
            }

            animator.Play(coinImage.transform.position, finalCoinCount, onCompleted);
        }


        public override void Show()
        {
            base.Show();
            PrepareVisuals();
        }

        protected override void OnShown()
        {
            base.OnShown();
            AnimateVisuals();
        }

        protected override void OnDestroy()
        {
            _animationSequence?.Kill();
            if (claim != null)
                claim.onClick.RemoveAllListeners();
            if (claimDouble != null)
                claimDouble.onClick.RemoveAllListeners();

            base.OnDestroy();
        }

        private void AnimateVisuals()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            var animator = EnsureCoinFlyAnimatorInstance();
            if (animator != null)
                animator.PlayHolderIntro();
            _animationSequence.Insert(stepDelay, headerText.DOFade(1f, durations).SetEase(Ease.Linear));
            _animationSequence.Insert(stepDelay * 2f, coinImage.transform.DOScale(1, durations).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 2f, coinWonAmountText.transform.DOScale(1, durations).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 3f, claimDouble.transform.DOScale(1, durations).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 4f, claim.transform.DOScale(1, durations).SetEase(Ease.OutBack));
        }

        private void PrepareVisuals()
        {
            var animator = EnsureCoinFlyAnimatorInstance();
            if (animator != null)
                animator.PrepareHolderIntro();
            headerText.alpha = 0f;
            coinImage.transform.localScale = Vector3.zero;
            coinWonAmountText.transform.localScale = Vector3.zero;
            claimDouble.transform.localScale = Vector3.zero;
            claim.transform.localScale = Vector3.zero;
        }

        private CoinFlyAnimator EnsureCoinFlyAnimatorInstance()
        {
            if (_coinFlyAnimatorInstance != null)
                return _coinFlyAnimatorInstance;

            if (coinFlyAnimator == null || panel == null)
                return null;

            if (coinFlyAnimator.transform.IsChildOf(transform))
            {
                _coinFlyAnimatorInstance = coinFlyAnimator;
            }
            else
            {
                _coinFlyAnimatorInstance = Instantiate(coinFlyAnimator, panel);
            }

            _coinFlyAnimatorInstance.SetParentTransform(panel);
            return _coinFlyAnimatorInstance;
        }

    }
}