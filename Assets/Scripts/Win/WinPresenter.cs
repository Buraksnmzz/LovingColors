using Gameplay;
using General;
using Level;
using SavedData;
using UI.General;

namespace Win
{
    public class WinPresenter : BasePresenter<WinView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.NextButtonClicked += OnNextButtonClicked;
        }

        private void OnNextButtonClicked()
        {
            _uiService.HidePopup<GameplayPresenter>(false);
            _uiService.ShowPopup<GameplayPresenter>();
            _uiService.HidePopup<WinPresenter>();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.NextButtonClicked -= OnNextButtonClicked;
            }

            base.Cleanup();
        }

        public override void ViewShown()
        {
            base.ViewShown();
        }
    }
}