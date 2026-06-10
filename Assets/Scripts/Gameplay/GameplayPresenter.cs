using Gameplay.Levels;
using Level;
using SavedData;
using General;
using UI.General;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private ISavedDataService _savedDataService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            View.Shown += OnViewShownCompleted;
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
            }

            base.Cleanup();
        }

        private void OnViewShownCompleted()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevelId = levelProgressModel.CurrentLevelIndex + 4;

            if (!View.TryGetLevelDefinition(currentLevelId, out var levelDefinition))
            {
                UnityEngine.Debug.LogError($"Could not build gameplay board for level {currentLevelId}.");
                return;
            }

            View.InitializeBoard(levelDefinition);
        }
    }
}
