using DailyChallenge;
using Gameplay;
using General;
using MainMenu;
using UI.General;

namespace Quit
{
    public class QuitPresenter : BasePresenter<QuitView>
    {
        private IUIService _uiService;
        private IDailyChallengeService _dailyChallengeService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            View.YesButtonClicked += OnYesButtonClicked;
        }

        private void OnYesButtonClicked()
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
            _uiService.HidePopup<QuitPresenter>();
        }
    }
}