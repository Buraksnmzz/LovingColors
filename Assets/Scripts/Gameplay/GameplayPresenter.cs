using DailyChallenge;
using GameConfig;
using Gameplay.Levels;
using Level;
using SavedData;
using General;
using General.EventDispatcher;
using MainMenu;
using Sound;
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
        private IEventDispatcherService _eventDispatcherService;
        private ISoundService _soundService;
        private IHapticService _hapticService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _hapticService = ServiceLocator.GetService<IHapticService>();
            View.Shown += OnViewShownCompleted;
            View.Solved += OnViewSolved;
            View.Completed += OnViewCompleted;
            View.MovesChanged += OnViewMovesChanged;
            View.MoveLimitReached += OnViewMoveLimitReached;
            View.DebugLevelStepRequested += OnDebugLevelStepRequested;
            View.BackButtonClicked += OnBackButtonClicked;
            _eventDispatcherService.AddListener<ContinueWithCoinSignal>(OnContinueWithCoinSignal);
            _eventDispatcherService.AddListener<ContinueWithRewardedSignal>(OnContinueWithRewardedSignal);
            _eventDispatcherService.AddListener<RestartButtonClickSignal>(OnRestartButtonClick);
        }

        private void OnRestartButtonClick(RestartButtonClickSignal obj)
        {
            _uiService.HidePopup<DailyChallengeLosePresenter>();
            RestartGame();
        }

        private void OnContinueWithRewardedSignal(ContinueWithRewardedSignal obj)
        {
            HandleAddMoves();
        }

        private void OnContinueWithCoinSignal(ContinueWithCoinSignal obj)
        {
            HandleAddMoves();
        }

        private void HandleAddMoves()
        {
            var extraMoves = _savedDataService.GetModel<RemoteConfigModel>().ExtraMovesCount;
            _soundService.PlaySound(ClipName.PowerUp);
            _hapticService.HapticLow();
            View.AddExtraMoves(extraMoves);
            _uiService.HidePopup<DailyChallengeLosePresenter>();
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
                View.MoveLimitReached -= OnViewMoveLimitReached;
                View.DebugLevelStepRequested -= OnDebugLevelStepRequested;
                View.BackButtonClicked -= OnBackButtonClicked;
            }

            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<ContinueWithCoinSignal>(OnContinueWithCoinSignal);
                _eventDispatcherService.RemoveListener<ContinueWithRewardedSignal>(OnContinueWithRewardedSignal);
                _eventDispatcherService.RemoveListener<RestartButtonClickSignal>(OnRestartButtonClick);
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
            View.InitializeBoard(levelDefinition, true);
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
            View.InitializeBoard(levelDefinition, false);
            if (ShouldShowFirstLevelTutorial(currentLevelIndex))
            {
                View.StartFirstLevelTutorial();
            }
        }

        private bool ShouldShowFirstLevelTutorial(int levelIndex)
        {
            return levelIndex == 0 && PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 0;
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

        private void OnViewMoveLimitReached()
        {
            if (!_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                return;
            }

            _uiService.ShowPopup<DailyChallengeLosePresenter>();
        }

        private void RestartGame()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                LoadDailyChallengeLevel();
                return;
            }

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            LoadLevelAtIndex(levelProgressModel.CurrentLevelIndex, true);
        }

        private void OnViewCompleted()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                _dailyChallengeService.CompletePlayedDay();
                _uiService.ShowPopup<DailyChallengeWinPresenter>();
                return;
            }

            if (PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 0)
            {
                PlayerPrefs.SetInt(StringConstants.IsTutorialShown, 1);
                PlayerPrefs.Save();
            }

            _uiService.ShowPopup<WinPresenter>();
        }
    }
}
