using General;
using Sound;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeReward1Presenter : BasePresenter<DailyChallengeReward1View>
    {
        private IUIService _uiService;
        private ISoundService _soundService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.ContinueClicked += OnContinueClicked;
            View.CompletedImageAnimationStarted += OnCompletedImageAnimationStarted;
            _soundService = ServiceLocator.GetService<ISoundService>();
        }

        private void OnCompletedImageAnimationStarted()
        {
            _soundService.PlaySound(ClipName.DailyChallengeReward);
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