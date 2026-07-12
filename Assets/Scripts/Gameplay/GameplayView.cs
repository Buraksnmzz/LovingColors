
using System;
using DG.Tweening;
using Gameplay.Levels;
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
        public event Action<int> DebugLevelStepRequested;

        [SerializeField] private Button debugNextButton;
        [SerializeField] private Button debugNext10Button;
        [SerializeField] private Button debugPrevButton;
        [SerializeField] private Button debugPrev10Button;
        [SerializeField] private Button debugCompleteButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI dcDateText;
        [SerializeField] private TextMeshProUGUI movesText;
        [SerializeField] private Image handImage;
        [SerializeField] private Image tutorialInfoImage;

        private const float MovesTextFadeDuration = 0.25f;

        private Board _board;
        private CanvasGroup _canvasGroup;
        private Sequence _tutorialHandSequence;
        private bool _isDailyChallenge;
        private bool _hasShownDailyChallengeTargetMoves;

        protected override void Awake()
        {
            base.Awake();
            _board = GetComponentInChildren<Board>(true);
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
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
                _board.TutorialDragRequested += PlayTutorialDrag;
                _board.TutorialTapRequested += PlayTutorialTap;
                _board.TutorialHandHideRequested += HideTutorialHand;
                _board.TutorialCompleted += OnBoardTutorialCompleted;
            }
        }

        private void PlayTutorialDrag(Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            PrepareTutorialHand(startWorldPosition, 0f);

            var handRectTransform = handImage.rectTransform;
            _tutorialHandSequence = DOTween.Sequence();
            _tutorialHandSequence.Append(handImage.DOFade(1f, 0.2f));
            _tutorialHandSequence.Join(tutorialInfoImage.DOFade(1f, 0.2f));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one * 1.12f, 0.15f).SetEase(Ease.OutBack));
            _tutorialHandSequence.Append(handRectTransform.DOMove(targetWorldPosition, 0.5f).SetEase(Ease.InOutQuad));
            _tutorialHandSequence.Append(handRectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
            _tutorialHandSequence.AppendInterval(0.35f);
            _tutorialHandSequence.Append(handImage.DOFade(0f, 0.25f));
            _tutorialHandSequence.Join(tutorialInfoImage.DOFade(0f, 0.25f));
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
            tutorialInfoImage.gameObject.SetActive(true);
            handImage.DOKill();
            tutorialInfoImage.DOKill();
            handRectTransform.DOKill();
            handImage.color = new Color(handImage.color.r, handImage.color.g, handImage.color.b, alpha);
            tutorialInfoImage.color = new Color(tutorialInfoImage.color.r, tutorialInfoImage.color.g, tutorialInfoImage.color.b, alpha);
            handRectTransform.position = worldPosition;
            handRectTransform.localScale = Vector3.one;
            handRectTransform.localRotation = Quaternion.identity;
        }

        private void HideTutorialHand()
        {
            _tutorialHandSequence?.Kill();
            handImage.DOKill();
            tutorialInfoImage.DOKill();
            handImage.rectTransform.DOKill();
            handImage.color = new Color(handImage.color.r, handImage.color.g, handImage.color.b, 0f);
            tutorialInfoImage.color = new Color(tutorialInfoImage.color.r, tutorialInfoImage.color.g, tutorialInfoImage.color.b, 0f);
            handImage.rectTransform.localScale = Vector3.one;
            handImage.rectTransform.localRotation = Quaternion.identity;
            handImage.gameObject.SetActive(false);
            tutorialInfoImage.gameObject.SetActive(false);
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
        public void SetDifficultyText(string difficulty)
        {
            difficultyText.text = difficulty;
        }

        public void SetDailyChallengeInfo(bool isDailyChallenge, string dateText)
        {
            _isDailyChallenge = isDailyChallenge;
            _hasShownDailyChallengeTargetMoves = false;

            if (levelText != null)
            {
                levelText.gameObject.SetActive(!isDailyChallenge);
            }

            if (difficultyText != null)
            {
                difficultyText.gameObject.SetActive(!isDailyChallenge);
            }

            if (dcDateText != null)
            {
                dcDateText.gameObject.SetActive(isDailyChallenge);
                dcDateText.text = dateText;
            }

            if (movesText != null)
            {
                movesText.gameObject.SetActive(isDailyChallenge);
                movesText.text = isDailyChallenge ? "0/0" : string.Empty;
                movesText.DOKill();
                movesText.alpha = isDailyChallenge ? 0f : 1f;
            }
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
            _board?.UseHint();
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

        private void OnBoardTutorialCompleted()
        {
            HideTutorialHand();
        }
    }
}
