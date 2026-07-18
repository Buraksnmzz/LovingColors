using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Home
{
    public class HomeView : BaseView
    {
        public event Action PlayLevelButtonClicked;
        public event Action RemoveAdsButtonClicked;
        public event Action CoinButtonClicked;
        public event Action DailyChallengeButtonClicked;

        [SerializeField] private Transform topBar;
        [SerializeField] private Transform pawnObject;
        [SerializeField] private Button playLevelButton;
        [SerializeField] private GameObject playButtonNormalImage;
        [SerializeField] private GameObject playButtonHardImage;
        [SerializeField] private GameObject playButtonSuperHardImage;

        [SerializeField] private Button removeAdsButton;
        [SerializeField] private Button coinButton;
        [SerializeField] private Button dailyChallengeButton;
        [SerializeField] private TextMeshProUGUI currentLevelText;
        [SerializeField] private TextMeshProUGUI currentLevelTextHard;
        [SerializeField] private TextMeshProUGUI currentLevelTextSuperHard;
        [SerializeField] private TextMeshProUGUI coinCount;
        [SerializeField] private CurrentFrame currentFrame;
        [SerializeField] private Frame[] nextFrames;
        [SerializeField] private Frame[] previousFrames;
        [SerializeField] private RectTransform frameContent;
        [SerializeField] private VerticalLayoutGroup frameContentLayoutGroup;
        [SerializeField] private RectTransform playLevelButtonRectTransform;
        [SerializeField] private float currentFrameBottomOffset;

        private float _initialFrameContentPositionY;
        private Sequence _introSequence;
        private Vector3 _topBarInitialLocalPosition;
        private Vector3 _pawnInitialLocalPosition;
        private Vector3 _playLevelButtonInitialScale;
        private Vector3 _removeAdsButtonInitialScale;
        private Vector3 _pawnInitialScale;
        private CanvasGroup _pawnCanvasGroup;

        private const float TopBarStartOffsetY = 220f;
        private const float PawnStartOffsetY = 400f;
        private const float TopBarMoveDuration = 0.45f;
        private const float PlayLevelButtonScaleDuration = 0.4f;
        private const float RemoveAdsScaleDuration = 0.5f;
        private const float PawnStartDelay = 0.2f;
        private const float PawnMoveDuration = 0.3f;
        private const float PawnFadeDuration = 0.22f;
        private const float PawnSquashDuration = 0.12f;

        public int NextFrameCount => nextFrames.Length;
        public int PreviousFrameCount => previousFrames.Length;

        protected override void Awake()
        {
            base.Awake();
            _initialFrameContentPositionY = frameContent.anchoredPosition.y;
            _topBarInitialLocalPosition = topBar.localPosition;
            _pawnInitialLocalPosition = pawnObject.localPosition;
            _playLevelButtonInitialScale = playLevelButton.transform.localScale;
            _removeAdsButtonInitialScale = removeAdsButton.transform.localScale;
            _pawnInitialScale = pawnObject.localScale;
            _pawnCanvasGroup = pawnObject.GetComponent<CanvasGroup>();
            if (_pawnCanvasGroup == null)
            {
                _pawnCanvasGroup = pawnObject.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            playLevelButton.onClick.AddListener(() => PlayLevelButtonClicked?.Invoke());
            removeAdsButton.onClick.AddListener(() => RemoveAdsButtonClicked?.Invoke());
            coinButton.onClick.AddListener(() => CoinButtonClicked?.Invoke());
            dailyChallengeButton.onClick.AddListener(()=>DailyChallengeButtonClicked?.Invoke());
        }

        protected override void OnShown()
        {
            base.OnShown();
            PlayIntroAnimation();
        }
        
        

        protected override void OnHidden()
        {
            base.OnHidden();
            KillIntroAnimation();
            ResetIntroAnimationState();
        }

        protected override void OnDestroy()
        {
            KillIntroAnimation();
            base.OnDestroy();
        }

        public void SetDifficultyView(LevelDifficultyType levelDifficultyType)
        {
            playButtonNormalImage.SetActive(false);
            playButtonHardImage.SetActive(false);
            playButtonSuperHardImage.SetActive(false);
            if (levelDifficultyType == LevelDifficultyType.Normal)
                playButtonNormalImage.SetActive(true);

            else if (levelDifficultyType == LevelDifficultyType.Hard)
                playButtonHardImage.SetActive(true);

            else
                playButtonSuperHardImage.SetActive(true);
        }

        public void SetNoAdsView(bool isPurchased)
        {
            removeAdsButton.gameObject.SetActive(!isPurchased);
        }

        public void SetCoinText(int coins)
        {
            coinCount.text = coins.ToString();
        }

        public void SetLevelText(string levelText)
        {
            currentLevelText.text = levelText;
            currentLevelTextHard.text = levelText;
            currentLevelTextSuperHard.text = levelText;
        }

        public void SetFrameContentPositionYOffset(float yOffset)
        {
            frameContent.anchoredPosition = new Vector2(frameContent.anchoredPosition.x, _initialFrameContentPositionY + yOffset);
        }

        public void SetLevelFrames(int currentLevelNumber, LevelDifficultyType currentDifficultyType,
            LevelDifficultyType[] nextDifficultyTypes, LevelDifficultyType[] previousDifficultyTypes)
        {
            currentFrame.SetLevel(currentLevelNumber, currentDifficultyType);
            currentFrame.SetPipeVisible(false);

            for (var index = 0; index < nextFrames.Length; index++)
            {
                var nextFrame = nextFrames[index];
                var nextLevelNumber = currentLevelNumber + index + 1;
                nextFrame.SetVisible(true);
                nextFrame.SetLevel(nextLevelNumber, nextDifficultyTypes[index], false);
                nextFrame.SetPipeVisible(false);
            }

            for (var index = 0; index < previousFrames.Length; index++)
            {
                var previousFrame = previousFrames[index];
                var previousLevelNumber = currentLevelNumber - index - 1;
                var hasPreviousLevel = previousLevelNumber >= 1;
                previousFrame.SetVisible(hasPreviousLevel);
                if (!hasPreviousLevel)
                    continue;

                previousFrame.SetLevel(previousLevelNumber, previousDifficultyTypes[index], true);
                previousFrame.SetPipeVisible(false);
            }

            SetLastActiveFramePipeVisible();
        }

        private void SetLastActiveFramePipeVisible()
        {
            for (var index = frameContent.childCount - 1; index >= 0; index--)
            {
                var child = frameContent.GetChild(index);
                if (!child.gameObject.activeSelf)
                    continue;

                if (child.TryGetComponent<Frame>(out var frame))
                {
                    frame.SetPipeVisible(true);
                }
                else if (child.TryGetComponent<CurrentFrame>(out var activeCurrentFrame))
                {
                    activeCurrentFrame.SetPipeVisible(true);
                }

                return;
            }
        }

        private void PlayIntroAnimation()
        {
            KillIntroAnimation();
            ResetIntroAnimationState();

            _introSequence = DOTween.Sequence();
            _introSequence.Join(topBar.DOLocalMove(_topBarInitialLocalPosition, TopBarMoveDuration).SetEase(Ease.OutBack));
            _introSequence.Join(playLevelButton.transform.DOScale(_playLevelButtonInitialScale, PlayLevelButtonScaleDuration)
                .SetEase(Ease.OutBack));

            if (removeAdsButton.gameObject.activeSelf)
            {
                _introSequence.Join(removeAdsButton.transform.DOScale(_removeAdsButtonInitialScale, RemoveAdsScaleDuration)
                    .SetEase(Ease.OutBack));
            }

            _introSequence.Insert(PawnStartDelay,
                pawnObject.DOLocalMove(_pawnInitialLocalPosition, PawnMoveDuration).SetEase(Ease.OutQuad));
            _introSequence.Insert(PawnStartDelay,
                _pawnCanvasGroup.DOFade(1f, PawnFadeDuration).SetEase(Ease.OutSine));
            _introSequence.Append(pawnObject.DOScale(new Vector3(_pawnInitialScale.x * 1.1f, _pawnInitialScale.y * 0.9f, _pawnInitialScale.z), PawnSquashDuration)
                .SetEase(Ease.OutQuad));
            _introSequence.Append(pawnObject.DOScale(new Vector3(_pawnInitialScale.x * 0.95f, _pawnInitialScale.y * 1.05f, _pawnInitialScale.z), PawnSquashDuration)
                .SetEase(Ease.OutQuad));
            _introSequence.Append(pawnObject.DOScale(_pawnInitialScale, PawnSquashDuration).SetEase(Ease.OutQuad));
            _introSequence.OnComplete(StartAutoLoopButtonAnimation);
        }

        private void ResetIntroAnimationState()
        {
            topBar.localPosition = _topBarInitialLocalPosition + Vector3.up * TopBarStartOffsetY;
            playLevelButton.transform.localScale = Vector3.zero;
            removeAdsButton.transform.localScale = removeAdsButton.gameObject.activeSelf ? Vector3.zero : _removeAdsButtonInitialScale;
            pawnObject.localPosition = _pawnInitialLocalPosition + Vector3.up * PawnStartOffsetY;
            pawnObject.localScale = _pawnInitialScale;
            _pawnCanvasGroup.alpha = 0f;
        }

        private void KillIntroAnimation()
        {
            _introSequence?.Kill();
            _introSequence = null;
            topBar.DOKill();
            playLevelButton.transform.DOKill();
            removeAdsButton.transform.DOKill();
            pawnObject.DOKill();
            _pawnCanvasGroup.DOKill();
        }
    }
}