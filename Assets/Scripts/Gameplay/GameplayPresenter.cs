using DailyChallenge;
using Gameplay.Levels;
using Level;
using SavedData;
using General;
using MainMenu;
using UI.General;
using UnityEngine;
using Win;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private ILevelService _levelService;
        private IDailyChallengeService _dailyChallengeService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            View.Shown += OnViewShownCompleted;
            View.Solved += OnViewSolved;
            View.Completed += OnViewCompleted;
            View.MovesChanged += OnViewMovesChanged;
            View.DebugLevelStepRequested += OnDebugLevelStepRequested;
            View.BackButtonClicked += OnBackButtonClicked;
        }

        private void OnBackButtonClicked()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                _dailyChallengeService.ClearActiveDailyChallengeGame();
                _uiService.HidePopup<GameplayPresenter>();
                _uiService.ShowPopup<DailyChallengePresenter>();
                return;
            }

            _uiService.HidePopup<GameplayPresenter>();
            _uiService.ShowPopup<HomePresenter>();
        }

        public override void ViewShown()
        {
            base.ViewShown();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.Shown -= OnViewShownCompleted;
                View.Solved -= OnViewSolved;
                View.Completed -= OnViewCompleted;
                View.MovesChanged -= OnViewMovesChanged;
                View.DebugLevelStepRequested -= OnDebugLevelStepRequested;
                View.BackButtonClicked -= OnBackButtonClicked;
            }

            base.Cleanup();
        }

        private void OnViewShownCompleted()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                LoadDailyChallengeLevel();
                return;
            }

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            LoadLevelAtIndex(levelProgressModel.CurrentLevelIndex, true);
        }

        private void OnDebugLevelStepRequested(int levelStep)
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
                return;

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var nextLevelIndex = Mathf.Max(0, levelProgressModel.CurrentLevelIndex + levelStep);
            LoadLevelAtIndex(nextLevelIndex, false);
        }

        private void LoadDailyChallengeLevel()
        {
            var levelId = _dailyChallengeService.GetPlayedLevelId();
            LevelDefinition levelDefinition;
            while (!_levelService.TryGetDailyChallengeLevelById(levelId, out levelDefinition))
            {
                levelId--;
                if (levelId <= 0)
                    return;
            }

            View.SetDailyChallengeInfo(true, _dailyChallengeService.GetPlayedDateText());
            View.InitializeBoard(levelDefinition);
        }

        private void LoadLevelAtIndex(int levelIndex, bool clampToPreviousValidLevel)
        {
            View.SetDailyChallengeInfo(false, string.Empty);
            View.SetLevelText(levelIndex + 1);

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevelIndex = Mathf.Max(0, levelIndex);
            var currentLevelId = currentLevelIndex + 1;
            LevelDefinition levelDefinition;

            while (!_levelService.TryGetLevelById(currentLevelId, out levelDefinition))
            {
                if (!clampToPreviousValidLevel || currentLevelIndex == 0)
                {
                    return;
                }

                currentLevelIndex--;
                currentLevelId = currentLevelIndex + 1;
            }

            if (currentLevelIndex != levelProgressModel.CurrentLevelIndex)
            {
                levelProgressModel.CurrentLevelIndex = currentLevelIndex;
                _savedDataService.SaveData(levelProgressModel);
            }
            View.SetDifficultyText(levelDefinition.Difficulty.ToString());
            View.InitializeBoard(levelDefinition);
        }

        private void OnViewSolved()
        {
            View.SetInteractionLocked(true);
        }

        private void OnViewMovesChanged(int moveCount, int totalMoveCount)
        {
            if (!_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                return;
            }

            View.SetMovesText(moveCount, totalMoveCount);
        }

        private void OnViewCompleted()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                _dailyChallengeService.CompletePlayedDay();
                _uiService.ShowPopup<DailyChallengeWinPresenter>();
                return;
            }

            _uiService.ShowPopup<WinPresenter>();
        }
    }
}
