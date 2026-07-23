
using System;
using DG.Tweening;
using Gameplay.Levels;
using Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class GameplayView : BaseView
    {
        public event Action Shown;
        public event Action Solved;
        public event Action Completed;
        public event Action BackButtonClicked;
        public event Action<int, int> MovesChanged;
        public event Action MoveLimitReached;
        public event Action HintClicked;
        public event Action<int> DebugLevelStepRequested;

        [SerializeField] private Button debugNextButton;
        [SerializeField] private Button debugNext10Button;
        [SerializeField] private Button debugPrevButton;
        [SerializeField] private Button debugPrev10Button;
        [SerializeField] private Button debugCompleteButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject dcDate;
        [SerializeField] private GameObject dcLevelInfo;
        [SerializeField] private GameObject normalLevelInfo;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI dcDateText;
        [SerializeField] private TextMeshProUGUI movesText;
        [SerializeField] private TextMeshProUGUI hintAmountText;
        [SerializeField] private GameObject addHintImage;
        [SerializeField] private Image handImage;
        [SerializeField] private Image backButtonImage;
        [SerializeField] private Image retryButtonImage;
        [SerializeField] private Image levelInfoImage;
        [SerializeField] private Image dcLevelInfoImage;
        [SerializeField] private Image hintButtonImage;
        [SerializeField] private Sprite backButtonNormalSprite;
        [SerializeField] private Sprite backButtonHardSprite;
        [SerializeField] private Sprite backButtonExtremeSprite;
        [SerializeField] private Sprite retryButtonNormalSprite;
        [SerializeField] private Sprite retryButtonHardSprite;
        [SerializeField] private Sprite retryButtonExtremeSprite;
        [SerializeField] private Sprite levelInfoNormalSprite;
        [SerializeField] private Sprite levelInfoHardSprite;
        [SerializeField] private Sprite levelInfoExtremeSprite;
        [SerializeField] private Sprite hintButtonNormalSprite;
        [SerializeField] private Sprite hintButtonHardSprite;
        [SerializeField] private Sprite hintButtonExtremeSprite;
        [SerializeField] private Sprite backgroundNormalSprite;
        [SerializeField] private Sprite backgroundHardSprite;
        [SerializeField] private Sprite backgroundExtremeSprite;
        [SerializeField] private Sprite movesNormalSprite;
        [SerializeField] private Sprite movesHardSprite;
        [SerializeField] private Sprite movesExtremeSprite;

        [SerializeField] private Image dcStampImage;
        [SerializeField] private Transform dcStampTransform;
        [SerializeField] private TextMeshProUGUI dcStampPlayingText;
        [SerializeField] private TextMeshProUGUI dcStampDateText;




        private const float MovesTextFadeDuration = 0.45f;
        private const float DailyChallengeStampScaleDuration = 0.35f;
        private const float DailyChallengeStampFadeDuration = 0.5f;
        private const float DailyChallengeStampTextFadeDuration = 0.3f;
        private const float DailyChallengeStampExitDelay = 1f;
        private const float DailyChallengeStampExitDuration = 0.5f;
        private const float DailyChallengeStampExitOffset = 1000f;

        private Board _board;
        private CanvasGroup _canvasGroup;
        private Sequence _tutorialHandSequence;
        private Sequence _dailyChallengeStampSequence;
        private bool _isDailyChallenge;
        private bool _hasShownDailyChallengeTargetMoves;
        private Vector3 _dailyChallengeStampInitialPosition;
        private bool _hasDailyChallengeStampInitialPosition;
        private Button[] _debugButtons;

        protected override void Awake()
        {
            base.Awake();
            _board = GetComponentInChildren<Board>(true);
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _debugButtons = new[]
            {
                debugNextButton,
                debugNext10Button,
                debugPrevButton,
                debugPrev10Button,
                debugCompleteButton
            };

            ConfigureDebugButtonsToIgnoreInteractionLock();

            debugNextButton.onClick.AddListener(OnDebugNextButtonClick);
            debugPrevButton.onClick.AddListener(OnDebugPrevButtonClick);
            debugNext10Button.onClick.AddListener(OnDebugNext10ButtonClick);
            debugPrev10Button.onClick.AddListener(OnDebugPrev10ButtonClick);
            debugCompleteButton.onClick.AddListener(OnDebugCompleteButtonClick);
            hintButton.onClick.AddListener(OnHintButtonClick);
            backButton.onClick.AddListener(() => BackButtonClicked?.Invoke());
            if (_board != null)
            {
                _board.Solved += OnBoardSolved;
                _board.WinSequenceCompleted += OnBoardCompleted;
                _board.MovesChanged += OnBoardMovesChanged;
                _board.MoveLimitReached += OnBoardMoveLimitReached;
                _board.ShuffleStarted += OnBoardShuffleStarted;
                _board.ShuffleCompleted += OnBoardShuffleCompleted;
                _board.TutorialDragRequested += PlayTutorialDrag;
                _board.TutorialTapRequested += PlayTutorialTap;
                _board.TutorialHandHideRequested += HideTutorialHand;
                _board.TutorialCompleted += OnBoardTutorialCompleted;
            }

            if (dcStampTransform != null)
            {
                _dailyChallengeStampInitialPosition = dcStampTransform.localPosition;
                _hasDailyChallengeStampInitialPosition = true;
            }
        }

        private void PlayTutorialDrag(Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            PrepareTutorialHand(startWorldPosition, 0f);

            var handRectTransform = handImage.rectTransform;
            _tutorialHandSequence = DOTween.Sequence();
            _tutorialHandSequence.Append(handImage.DOFade(1f, 0.2f));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one * 1.12f, 0.15f).SetEase(Ease.OutBack));
            _tutorialHandSequence.Append(handRectTransform.DOMove(targetWorldPosition, 0.5f).SetEase(Ease.InOutQuad));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
            _tutorialHandSequence.AppendInterval(0.35f);
            _tutorialHandSequence.Append(handImage.DOFade(0f, 0.25f));
            _tutorialHandSequence.AppendCallback(() => handRectTransform.position = startWorldPosition);
            _tutorialHandSequence.AppendInterval(0.2f);
            _tutorialHandSequence.SetLoops(-1, LoopType.Restart);
            _tutorialHandSequence.OnKill(() => _tutorialHandSequence = null);
        }

        private void PlayTutorialTap(Vector3 worldPosition)
        {
            PrepareTutorialHand(worldPosition, 1f);

            var handRectTransform = handImage.rectTransform;
            _tutorialHandSequence = DOTween.Sequence();
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one * 1.15f, 0.25f).SetEase(Ease.OutQuad));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one * 0.9f, 0.2f).SetEase(Ease.InOutQuad));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
            _tutorialHandSequence.SetLoops(-1, LoopType.Restart);
            _tutorialHandSequence.OnKill(() => _tutorialHandSequence = null);
        }

        private void PrepareTutorialHand(Vector3 worldPosition, float alpha)
        {
            _tutorialHandSequence?.Kill();
            var handRectTransform = handImage.rectTransform;
            handImage.gameObject.SetActive(true);
            handImage.DOKill();
            handRectTransform.DOKill();
            handImage.color = new Color(handImage.color.r, handImage.color.g, handImage.color.b, alpha);
            handRectTransform.position = worldPosition;
            handRectTransform.localScale = Vector3.one;
            handRectTransform.localRotation = Quaternion.identity;
        }

        private void HideTutorialHand()
        {
            _tutorialHandSequence?.Kill();
            handImage.DOKill();
            handImage.rectTransform.DOKill();
            handImage.color = new Color(handImage.color.r, handImage.color.g, handImage.color.b, 0f);
            handImage.rectTransform.localScale = Vector3.one;
            handImage.rectTransform.localRotation = Quaternion.identity;
            handImage.gameObject.SetActive(false);
        }

        public void StartFirstLevelTutorial()
        {
            _board?.StartFirstLevelTutorial();
        }

        public void StopFirstLevelTutorial()
        {
            _board?.StopFirstLevelTutorial();
            HideTutorialHand();
        }

        private void OnDebugPrev10ButtonClick()
        {
            DebugLevelStepRequested?.Invoke(-10);
        }

        private void OnDebugNext10ButtonClick()
        {
            DebugLevelStepRequested?.Invoke(10);
        }

        public void SetLevelText(int level)
        {
            levelText.text = "Level " + level;
        }
        public void SetDifficultyText(LevelDifficultyType difficulty)
        {
            difficultyText.text = difficulty.ToString();
            difficultyText.gameObject.SetActive(difficulty != LevelDifficultyType.Normal);
            SetDifficultySprites(difficulty);
        }

        public void SetLevelInfoImages(bool isDailyChallenge)
        {
            levelInfoImage.gameObject.SetActive(!isDailyChallenge);
            dcLevelInfoImage.gameObject.SetActive(isDailyChallenge);
        }

        private void SetDifficultySprites(LevelDifficultyType difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficultyType.Hard:
                    SetImageSprite(backButtonImage, backButtonHardSprite);
                    SetImageSprite(retryButtonImage, retryButtonHardSprite);
                    SetImageSprite(levelInfoImage, levelInfoHardSprite);
                    SetImageSprite(hintButtonImage, hintButtonHardSprite);
                    SetImageSprite(backgroundImage, backgroundHardSprite);
                    SetImageSprite(dcLevelInfoImage, movesHardSprite);
                    break;
                case LevelDifficultyType.SuperHard:
                    SetImageSprite(backButtonImage, backButtonExtremeSprite);
                    SetImageSprite(retryButtonImage, retryButtonExtremeSprite);
                    SetImageSprite(levelInfoImage, levelInfoExtremeSprite);
                    SetImageSprite(hintButtonImage, hintButtonExtremeSprite);
                    SetImageSprite(backgroundImage, backgroundExtremeSprite);
                    SetImageSprite(dcLevelInfoImage, movesExtremeSprite);
                    break;
                default:
                    SetImageSprite(backButtonImage, backButtonNormalSprite);
                    SetImageSprite(retryButtonImage, retryButtonNormalSprite);
                    SetImageSprite(levelInfoImage, levelInfoNormalSprite);
                    SetImageSprite(hintButtonImage, hintButtonNormalSprite);
                    SetImageSprite(backgroundImage, backgroundNormalSprite);
                    SetImageSprite(dcLevelInfoImage, movesNormalSprite);
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

        public void SetDailyChallengeInfo(bool isDailyChallenge, string dateText)
        {
            _isDailyChallenge = isDailyChallenge;
            _hasShownDailyChallengeTargetMoves = false;
            ResetDailyChallengeStamp();
            levelText.gameObject.SetActive(!isDailyChallenge);
            difficultyText.gameObject.SetActive(!isDailyChallenge);
            dcDate.SetActive(isDailyChallenge);
            normalLevelInfo.SetActive(!isDailyChallenge);
            dcDateText.text = dateText;
            dcStampDateText.text = dateText;
            dcLevelInfo.SetActive(isDailyChallenge);
            movesText.text = isDailyChallenge ? "0/0" : string.Empty;
            movesText.DOKill();
            movesText.alpha = isDailyChallenge ? 0f : 1f;
        }

        public void SetMovesText(int moveCount, int totalMoveCount)
        {
            if (movesText == null)
            {
                return;
            }

            movesText.text = moveCount + "/" + totalMoveCount;
            if (_isDailyChallenge && !_hasShownDailyChallengeTargetMoves && totalMoveCount > 0)
            {
                _hasShownDailyChallengeTargetMoves = true;
                movesText.DOKill();
                movesText.DOFade(1f, MovesTextFadeDuration).SetEase(Ease.OutQuad);
            }
        }

        public void AddExtraMoves(int moveCount)
        {
            _board?.AddExtraMoves(moveCount);
        }

        private void OnDebugCompleteButtonClick()
        {
            _board?.CompleteImmediately();
        }

        protected override void OnDestroy()
        {
            if (_board != null)
            {
                _board.Solved -= OnBoardSolved;
                _board.WinSequenceCompleted -= OnBoardCompleted;
                _board.MovesChanged -= OnBoardMovesChanged;
                _board.MoveLimitReached -= OnBoardMoveLimitReached;
                _board.ShuffleStarted -= OnBoardShuffleStarted;
                _board.ShuffleCompleted -= OnBoardShuffleCompleted;
                _board.TutorialDragRequested -= PlayTutorialDrag;
                _board.TutorialTapRequested -= PlayTutorialTap;
                _board.TutorialHandHideRequested -= HideTutorialHand;
                _board.TutorialCompleted -= OnBoardTutorialCompleted;
            }

            debugNextButton.onClick.RemoveListener(OnDebugNextButtonClick);
            debugPrevButton.onClick.RemoveListener(OnDebugPrevButtonClick);
            debugNext10Button.onClick.RemoveListener(OnDebugNext10ButtonClick);
            debugPrev10Button.onClick.RemoveListener(OnDebugPrev10ButtonClick);
            debugCompleteButton.onClick.RemoveListener(OnDebugCompleteButtonClick);
            hintButton.onClick.RemoveListener(OnHintButtonClick);
            _dailyChallengeStampSequence?.Kill();
            StopFirstLevelTutorial();
            base.OnDestroy();
        }

        private void OnDebugPrevButtonClick()
        {
            DebugLevelStepRequested?.Invoke(-1);
        }

        private void OnDebugNextButtonClick()
        {
            DebugLevelStepRequested?.Invoke(1);
        }

        private void OnHintButtonClick()
        {
            HintClicked?.Invoke();
        }

        public void UseHint(int remainingHint)
        {
            _board.UseHint();
            SetHintAmount(remainingHint);
        }

        public void SetHintAmount(int amount)
        {
            hintAmountText.text = amount.ToString();

            if (amount == 0)
            {
                addHintImage.SetActive(true);
                hintAmountText.gameObject.SetActive(false);
            }
            else
            {
                addHintImage.SetActive(false);
                hintAmountText.gameObject.SetActive(true);
            }
        }

        protected override void OnShown()
        {
            base.OnShown();
            SetInteractionLocked(false);
            Shown?.Invoke();
        }

        public void InitializeBoard(LevelDefinition levelDefinition, bool isMoveLimitEnabled)
        {
            StopFirstLevelTutorial();
            if (levelDefinition == null)
            {
                return;
            }

            SetInteractionLocked(false);
            _board.Initialize(levelDefinition, isMoveLimitEnabled);
        }

        public void SetInteractionLocked(bool isLocked)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = !isLocked;
            _canvasGroup.blocksRaycasts = !isLocked;
        }

        private void ConfigureDebugButtonsToIgnoreInteractionLock()
        {
            if (_debugButtons == null)
            {
                return;
            }

            foreach (var button in _debugButtons)
            {
                if (button == null)
                {
                    continue;
                }

                var debugCanvasGroup = button.GetComponent<CanvasGroup>();
                if (debugCanvasGroup == null)
                {
                    debugCanvasGroup = button.gameObject.AddComponent<CanvasGroup>();
                }

                debugCanvasGroup.ignoreParentGroups = true;
                debugCanvasGroup.interactable = true;
                debugCanvasGroup.blocksRaycasts = true;
            }
        }

        private void OnBoardSolved()
        {
            SetInteractionLocked(true);
            Solved?.Invoke();
        }

        private void OnBoardCompleted()
        {
            Completed?.Invoke();
        }

        private void OnBoardMovesChanged(int moveCount, int totalMoveCount)
        {
            MovesChanged?.Invoke(moveCount, totalMoveCount);
        }

        private void OnBoardMoveLimitReached()
        {
            MoveLimitReached?.Invoke();
        }

        private void OnBoardShuffleStarted()
        {
            SetInteractionLocked(true);
        }

        private void OnBoardShuffleCompleted()
        {
            SetInteractionLocked(false);

            if (_isDailyChallenge)
            {
                PlayDailyChallengeStamp();
            }
        }

        private void ResetDailyChallengeStamp()
        {
            _dailyChallengeStampSequence?.Kill();

            if (dcStampImage == null || dcStampTransform == null || dcStampPlayingText == null || dcStampDateText == null)
            {
                return;
            }

            dcStampImage.DOKill();
            dcStampTransform.DOKill();
            dcStampPlayingText.DOKill();
            dcStampDateText.DOKill();
            if (_hasDailyChallengeStampInitialPosition)
            {
                dcStampTransform.localPosition = _dailyChallengeStampInitialPosition;
            }
            dcStampTransform.localScale = Vector3.one * 2f;
            dcStampImage.DOFade(0, 0);
            dcStampPlayingText.DOFade(0, 0);
            dcStampDateText.DOFade(0, 0);
        }

        private void PlayDailyChallengeStamp()
        {
            ResetDailyChallengeStamp();
            if (dcStampImage == null || dcStampTransform == null || dcStampPlayingText == null || dcStampDateText == null)
            {
                return;
            }

            var stampStartPosition = dcStampTransform.localPosition;
            _dailyChallengeStampSequence = DOTween.Sequence();
            _dailyChallengeStampSequence.Join(dcStampTransform.DOScale(Vector3.one, DailyChallengeStampScaleDuration).SetEase(Ease.OutBack));
            _dailyChallengeStampSequence.Join(dcStampImage.DOFade(1f, DailyChallengeStampFadeDuration));
            _dailyChallengeStampSequence.Append(dcStampPlayingText.DOFade(1f, DailyChallengeStampTextFadeDuration));
            _dailyChallengeStampSequence.Insert(_dailyChallengeStampSequence.Duration() - DailyChallengeStampTextFadeDuration * 0.5f,
                dcStampDateText.DOFade(1f, DailyChallengeStampTextFadeDuration));
            _dailyChallengeStampSequence.AppendInterval(DailyChallengeStampExitDelay);
            _dailyChallengeStampSequence.Append(dcStampImage.DOFade(0f, DailyChallengeStampExitDuration));
            _dailyChallengeStampSequence.Join(dcStampTransform.DOLocalMoveX(stampStartPosition.x + DailyChallengeStampExitOffset, DailyChallengeStampExitDuration)
                .SetEase(Ease.InQuad));
            _dailyChallengeStampSequence.OnKill(() => _dailyChallengeStampSequence = null);
        }

        private void OnBoardTutorialCompleted()
        {
            HideTutorialHand();
        }
    }
}
