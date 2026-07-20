using System;
using DG.Tweening;
using TMPro;
using UI.General;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Win
{
    public class WinView : BaseView
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button claimX2Button;
        [SerializeField] private TextMeshProUGUI nextButtonText;
        [SerializeField] private CoinFlyAnimator coinFlyAnimator;
        [SerializeField] private Transform header;
        [SerializeField] private Transform newBadgeHeader;
        [SerializeField] private Transform reward;
        [SerializeField] private Transform badge;
        [SerializeField] private Transform glow;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Image newBadgeImage;
        [SerializeField] private Image experienceFillImage;
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private float animateDuration = 0.35f;


        [SerializeField] private ParticleSystem fireworkParticle1;
        [SerializeField] private ParticleSystem fireworkParticle2;
        [SerializeField] private ParticleSystem fireworkParticle3;

        public event Action NextButtonClicked;
        public event Action ClaimButtonClicked;
        public event Action ClaimX2ButtonClicked;
        public event Action Hidden;
        public event Action IntroAnimationFinished;
        

        private Sequence _animationSequence;

        private void Start()
        {
            nextButton.onClick.AddListener(OnNextButtonClick);
            claimButton.onClick.AddListener(OnClaimButtonClick);
            claimX2Button.onClick.AddListener(OnClaimX2ButtonClick);
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            Hidden?.Invoke();
        }

        public void SetRewardText(int amount)
        {
            rewardText.SetText(amount.ToString());
        }

        public void SetCoinCount(int value)
        {
            coinFlyAnimator.SetCoinCount(value);
        }

        public void PlayCoinFly(int totalCoins, Action onCompleted = null)
        {
            coinFlyAnimator.Play(reward.position, totalCoins, onCompleted);
        }

        public void CompleteCoinFly()
        {
            coinFlyAnimator.Complete();
        }

        public void SetClaimButtonsInteractable(bool interactable)
        {
            claimButton.interactable = interactable;
            claimX2Button.interactable = interactable;
        }

        public void SetNextButtonText(string text)
        {
            nextButtonText.text = text;
        }

        public void SetBadgeProgress(Sprite badgeSprite, int experience, int targetExperience)
        {
            badgeImage.sprite = badgeSprite;
            experienceFillImage.fillAmount = targetExperience > 0
                ? Mathf.Clamp01((float)experience / targetExperience)
                : 0f;
            experienceText.text = $"{experience}/{targetExperience}";
        }

        public void PlayWinAnimation(Sprite badgeSprite, int previousExperience, int currentExperience, int targetExperience, int totalCoin)
        {
            StopAutoLoopButtonAnimation();
            SetNormalWinState();
            SetBadgeProgress(badgeSprite, previousExperience, targetExperience);
            _animationSequence = CreateSequence();
            var halfDuration = animateDuration * 0.5f;

            _animationSequence.Append(header.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            FadeCanvasGroup(header, 1f, animateDuration);
            _animationSequence.Insert(halfDuration, reward.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            _animationSequence.Append(badge.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            _animationSequence.Append(experienceFillImage.DOFillAmount(
                targetExperience > 0 ? Mathf.Clamp01((float)currentExperience / targetExperience) : 0f,
                0.5f));
            _animationSequence.AppendCallback(() => experienceText.text = $"{currentExperience}/{targetExperience}");
            _animationSequence.Append(nextButton.transform.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack)
                .OnStart(() => IntroAnimationFinished?.Invoke()));
            _animationSequence.InsertCallback(_animationSequence.Duration() - animateDuration * 0.5f, () => PlayCoinFly(totalCoin));
            _animationSequence.OnComplete(StartAutoLoopButtonAnimation);
            
        }

        public void PlayNewBadgeAnimation(Sprite badgeSprite)
        {
            StopAutoLoopButtonAnimation();
            SetNewBadgeState();
            newBadgeImage.sprite = badgeSprite;
            _animationSequence = CreateSequence();
            var halfDuration = animateDuration * 0.5f;

            _animationSequence.Append(newBadgeHeader.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            FadeCanvasGroup(newBadgeHeader, 1f, animateDuration);
            _animationSequence.Insert(halfDuration, glow.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(animateDuration, reward.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack).OnComplete(() => IntroAnimationFinished?.Invoke()));
            _animationSequence.Insert(animateDuration + halfDuration, claimX2Button.transform.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(animateDuration * 2f, claimButton.transform.DOScale(Vector3.one, animateDuration).SetEase(Ease.OutBack));
            _animationSequence.OnComplete(StartAutoLoopButtonAnimation);
        }

        public void PlayParticles()
        {
            DOVirtual.DelayedCall(0.5f, () => fireworkParticle1.Play());
            DOVirtual.DelayedCall(0.8f, () => fireworkParticle2.Play());
            DOVirtual.DelayedCall(1.1f, () => fireworkParticle3.Play());
        }

        protected override void OnDestroy()
        {
            _animationSequence?.Kill();
            nextButton.onClick.RemoveListener(OnNextButtonClick);
            claimButton.onClick.RemoveListener(OnClaimButtonClick);
            claimX2Button.onClick.RemoveListener(OnClaimX2ButtonClick);
            base.OnDestroy();
        }

        private void OnNextButtonClick()
        {
            NextButtonClicked?.Invoke();
        }

        private void OnClaimButtonClick()
        {
            ClaimButtonClicked?.Invoke();
        }

        private void OnClaimX2ButtonClick()
        {
            ClaimX2ButtonClicked?.Invoke();
        }

        private Sequence CreateSequence()
        {
            _animationSequence?.Kill();
            return DOTween.Sequence();
        }

        private void SetNormalWinState()
        {
            header.gameObject.SetActive(true);
            badge.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
            newBadgeHeader.gameObject.SetActive(false);
            glow.gameObject.SetActive(false);
            claimButton.gameObject.SetActive(false);
            claimX2Button.gameObject.SetActive(false);

            header.localScale = Vector3.one * 2f;
            SetCanvasGroupAlpha(header, 0f);
            reward.localScale = Vector3.zero;
            badge.localScale = Vector3.zero;
            nextButton.transform.localScale = Vector3.zero;
        }

        private void SetNewBadgeState()
        {
            header.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            newBadgeHeader.gameObject.SetActive(true);
            glow.gameObject.SetActive(true);
            claimButton.gameObject.SetActive(true);
            claimX2Button.gameObject.SetActive(true);

            newBadgeHeader.localScale = Vector3.one * 2f;
            SetCanvasGroupAlpha(newBadgeHeader, 0f);
            glow.localScale = Vector3.zero;
            reward.localScale = Vector3.zero;
            claimButton.transform.localScale = Vector3.zero;
            claimX2Button.transform.localScale = Vector3.zero;
            SetClaimButtonsInteractable(true);
        }

        private static void FadeCanvasGroup(Transform target, float endValue, float duration)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            canvasGroup?.DOFade(endValue, duration);
        }

        private static void SetCanvasGroupAlpha(Transform target, float value)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = value;
        }

    }
}