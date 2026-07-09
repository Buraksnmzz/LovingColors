using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeReward1Presenter : BasePresenter<DailyChallengeReward1View>
    {
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = General.ServiceLocator.GetService<IUIService>();
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
            _uiService.HidePopup<DailyChallengeReward1Presenter>();
            _uiService.ShowPopup<DailyChallengeReward2Presenter>();
        }
    }
}