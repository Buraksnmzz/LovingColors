using Gameplay.Levels;
using Level;
using SavedData;
using General;
using UI.General;
using UnityEngine;
using Win;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.Shown += OnViewShownCompleted;
            View.Solved += OnViewSolved;
            View.Completed += OnViewCompleted;
            View.DebugLevelStepRequested += OnDebugLevelStepRequested;
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
                View.DebugLevelStepRequested -= OnDebugLevelStepRequested;
            }

            base.Cleanup();
        }

        private void OnViewShownCompleted()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            LoadLevelAtIndex(levelProgressModel.CurrentLevelIndex, true);
        }

        private void OnDebugLevelStepRequested(int levelStep)
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var nextLevelIndex = Mathf.Max(0, levelProgressModel.CurrentLevelIndex + levelStep);
            LoadLevelAtIndex(nextLevelIndex, false);
        }

        private void LoadLevelAtIndex(int levelIndex, bool clampToPreviousValidLevel)
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevelIndex = Mathf.Max(0, levelIndex);
            var currentLevelId = currentLevelIndex + 1;
            LevelDefinition levelDefinition;

            while (!View.TryGetLevelDefinition(currentLevelId, out levelDefinition))
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

            View.InitializeBoard(levelDefinition);
        }

        private void OnViewSolved()
        {
            View.SetInteractionLocked(true);
        }

        private void OnViewCompleted()
        {
            _uiService.ShowPopup<WinPresenter>();
        }
    }
}
