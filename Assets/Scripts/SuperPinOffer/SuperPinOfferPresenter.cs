using General;
using General.EventDispatcher;
using UI.General;

namespace SuperPinOffer
{
    public class SuperPinOfferPresenter : BasePresenter<SuperPinOfferView>
    {
        private IAdsService _adsService;
        private IEventDispatcherService _eventDispatcherService;
        private IUIService _uiService;
        private bool _hasGrantedFreeSuperPin;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.PinButtonClicked += OnPinButtonClicked;
            View.NoButtonClicked += OnNoButtonClicked;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _hasGrantedFreeSuperPin = false;
            View.SetButtonsInteractable(true);
        }

        public override void ViewHidden()
        {
            base.ViewHidden();
            _eventDispatcherService?.Dispatch(new SuperPinOfferClosedSignal(_hasGrantedFreeSuperPin));
            _hasGrantedFreeSuperPin = false;
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.PinButtonClicked -= OnPinButtonClicked;
                View.NoButtonClicked -= OnNoButtonClicked;
            }

            base.Cleanup();
        }

        private void OnPinButtonClicked()
        {
            if (!_adsService.IsRewardedAvailable())
            {
                return;
            }

            View.SetButtonsInteractable(false);
            _adsService.GetReward(OnRewardedCompleted);
        }

        private void OnRewardedCompleted(bool isSuccess)
        {
            if (!isSuccess)
            {
                View.SetButtonsInteractable(true);
                return;
            }

            _hasGrantedFreeSuperPin = true;
            _uiService.HidePopup<SuperPinOfferPresenter>();
        }

        private void OnNoButtonClicked()
        {
            _uiService.HidePopup<SuperPinOfferPresenter>();
        }
    }
}