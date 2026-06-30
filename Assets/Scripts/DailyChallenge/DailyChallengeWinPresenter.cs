using Gameplay;
using General;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeWinPresenter : BasePresenter<DailyChallengeWinView>
    {
        private IDailyChallengeService _dailyChallengeService;
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.ContinueClicked += OnContinueClicked;
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.ContinueClicked -= OnContinueClicked;
            }

            base.Cleanup();
        }

        private void OnContinueClicked()
        {
            if (_dailyChallengeService.SelectFirstPlayableUncompletedDay())
            {
                _dailyChallengeService.StartSelectedDayGame();
                _uiService.HidePopup<GameplayPresenter>(false);
                _uiService.ShowPopup<GameplayPresenter>();
                _uiService.HidePopup<DailyChallengeWinPresenter>();
                return;
            }

            _uiService.HidePopup<GameplayPresenter>(false);
            _uiService.HidePopup<DailyChallengeWinPresenter>();
            _uiService.ShowPopup<DailyChallengePresenter>();
        }
    }
}