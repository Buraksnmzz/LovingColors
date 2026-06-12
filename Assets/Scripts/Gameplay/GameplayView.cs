
using System;
using General;
using Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class GameplayView : BaseView
    {
        public event Action Shown;
        public event Action Solved;
        public event Action Completed;
        public event Action<int> DebugLevelStepRequested;

        [SerializeField] private TextAsset levelConfig;
        [SerializeField] private Button debugNextButton;
        [SerializeField] private Button debugPrevButton;
        [SerializeField] private Button debugCompleteButton;
        [SerializeField] private Button hintButton;

        private Board _board;
        private LevelCatalog _levelCatalog;
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
            _levelCatalog = LevelCatalog.Parse(levelConfig);
            debugNextButton.onClick.AddListener(OnDebugNextButtonClick);
            debugPrevButton.onClick.AddListener(OnDebugPrevButtonClick);
            debugCompleteButton.onClick.AddListener(OnDebugCompleteButtonClick);
            hintButton.onClick.AddListener(OnHintButtonClick);
            if (_board != null)
            {
                _board.Solved += OnBoardSolved;
                _board.WinSequenceCompleted += OnBoardCompleted;
            }
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

        public bool TryGetLevelDefinition(int levelId, out LevelDefinition levelDefinition)
        {
            levelDefinition = null;
            return _levelCatalog != null && _levelCatalog.TryGetLevel(levelId, out levelDefinition);
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
    }
}
