using General;
using UI.General;

namespace PinBoostersActivated
{
    public class PinBoostersActivatedPresenter : BasePresenter<PinBoostersActivatedView>
    {
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.OkButtonClicked += OnOkButtonClicked;
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.OkButtonClicked -= OnOkButtonClicked;
            }

            base.Cleanup();
        }

        private void OnOkButtonClicked()
        {
            _uiService.HidePopup<PinBoostersActivatedPresenter>();
        }
    }
}