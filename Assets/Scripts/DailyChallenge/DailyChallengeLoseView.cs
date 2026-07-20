using System;
using DG.Tweening;
using Home;
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
        [SerializeField] private TextMeshProUGUI plusXMovesText;
        [SerializeField] private Image headerImage;
        [SerializeField] private Image middleImage;
        [SerializeField] private Sprite headerSpriteNormal;
        [SerializeField] private Sprite headerSpriteHard;
        [SerializeField] private Sprite headerSpriteExtreme;
        [SerializeField] private Sprite middleSpriteNormal;
        [SerializeField] private Sprite middleSpriteHard;
        [SerializeField] private Sprite middleSpriteExtreme;
        [SerializeField] private float stepDelay = 0.3f;
        [SerializeField] private float scaleDuration = 0.45f;
        [SerializeField] private float headerStartScale = 2f;

        public event Action RestartButtonClicked;
        public event Action AddMovesClicked;
        public event Action ContinueButtonClicked;
        private Sequence _animationSequence;

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

        public void SetPlusXMovesText(string xMovesText)
        {
            plusXMovesText.text = xMovesText;
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
            base.OnDestroy();
        }

        public void SetDifficultySprites(LevelDifficultyType difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficultyType.Hard:
                    SetImageSprite(headerImage, headerSpriteHard);
                    SetImageSprite(middleImage, middleSpriteHard);
                    break;
                case LevelDifficultyType.SuperHard:
                    SetImageSprite(headerImage, headerSpriteExtreme);
                    SetImageSprite(middleImage, middleSpriteExtreme);
                    break;
                default:
                    SetImageSprite(headerImage, headerSpriteNormal);
                    SetImageSprite(middleImage, middleSpriteNormal);
                    break;
            }
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private void AnimateVisuals()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            _animationSequence.Insert(0f, headerImage.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(0f, headerImage.DOFade(1f, scaleDuration).SetEase(Ease.Linear));
            _animationSequence.Insert(stepDelay, middleImage.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 2.5f, restartButton.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 2f, addMovesButton.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 1.5f, continueButton.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
        }

        private void PrepareVisuals()
        {
            headerImage.transform.localScale = Vector3.one * headerStartScale;
            var headerColor = headerImage.color;
            headerColor.a = 0f;
            headerImage.color = headerColor;
            middleImage.transform.localScale = Vector3.zero;
            restartButton.transform.localScale = Vector3.zero;
            addMovesButton.transform.localScale = Vector3.zero;
            continueButton.transform.localScale = Vector3.zero;
        }

    }
}
