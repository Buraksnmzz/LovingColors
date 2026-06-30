
using System;
using Gameplay.Levels;
using General;
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

        private Board _board;
        private CanvasGroup _canvasGroup;

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
            }
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
            }
        }

        public void SetMovesText(int moveCount, int totalMoveCount)
        {
            if (movesText == null)
            {
                return;
            }

            movesText.text = moveCount + "/" + totalMoveCount;
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
            }

            debugNextButton.onClick.RemoveListener(OnDebugNextButtonClick);
            debugPrevButton.onClick.RemoveListener(OnDebugPrevButtonClick);
            debugCompleteButton.onClick.RemoveListener(OnDebugCompleteButtonClick);
            hintButton.onClick.RemoveListener(OnHintButtonClick);
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

        public void InitializeBoard(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                return;
            }

            SetInteractionLocked(false);
            _board.Initialize(levelDefinition);
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
    }
}
