using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using Localization;
using SavedData;
using Services;
using UI.General;

namespace GetHint
{
    public class BoosterIntroPresenter : BasePresenterWithData<BoosterIntroView, GetHintPopupData>
    {
        private ISavedDataService _savedDataService;
        private IEventDispatcherService _eventDispatcherService;
        private ILocalizationService _localizationService;
        private IUIService _uiService;

        private GetHintPopupType _popupType = GetHintPopupType.Pin;
        private bool _isClaimInProgress;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            _uiService = ServiceLocator.GetService<IUIService>();

            View.GetBoosterButtonClicked += OnGetBoosterButtonClicked;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _isClaimInProgress = false;
            _eventDispatcherService.Dispatch(new BoosterIntroShownSignal(_popupType));
            RefreshView();
        }

        public override void ViewHidden()
        {
            _eventDispatcherService.Dispatch(new BoosterIntroClosedSignal(_popupType));
            base.ViewHidden();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.GetBoosterButtonClicked -= OnGetBoosterButtonClicked;
            }

            base.Cleanup();
        }

        protected override void OnDataSet()
        {
            base.OnDataSet();
            _popupType = Data.PopupType == GetHintPopupType.SuperPin
                ? GetHintPopupType.SuperPin
                : GetHintPopupType.Pin;

            if (View != null)
            {
                RefreshView();
            }
        }

        private void RefreshView()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();

            View.ConfigurePopup(_popupType);
            View.SetPinAmount(collectibleModel.TotalPins);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
            View.SetBoosterRewardAmountText(GetRewardAmount(remoteConfigModel));

            if (_popupType == GetHintPopupType.SuperPin)
            {
                View.SetHeaderAndInfoText(
                    _localizationService.GetLocalizedString(LocalizationStrings.SuperPin),
                    _localizationService.GetLocalizedString(LocalizationStrings.SuperPinInfo));
                return;
            }

            View.SetHeaderAndInfoText(
                _localizationService.GetLocalizedString(LocalizationStrings.Pin),
                _localizationService.GetLocalizedString(LocalizationStrings.PinInfo));
        }

        private void OnGetBoosterButtonClicked()
        {
            if (_isClaimInProgress)
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            var rewardAmount = GetRewardAmount(remoteConfigModel);

            if (rewardAmount <= 0)
            {
                _uiService.HidePopup<BoosterIntroPresenter>();
                return;
            }

            _isClaimInProgress = true;

            if (_popupType == GetHintPopupType.SuperPin)
            {
                collectibleModel.TotalSuperPins += rewardAmount;
            }
            else
            {
                collectibleModel.TotalPins += rewardAmount;
            }

            _savedDataService.SaveData(collectibleModel);

            try
            {
                View.PlayBoosterFly(OnBoosterFlyCompleted);
            }
            catch
            {
                OnBoosterFlyCompleted();
            }
        }

        private void OnBoosterFlyCompleted()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            View.SetPinAmount(collectibleModel.TotalPins);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);

            if (_popupType == GetHintPopupType.SuperPin)
            {
                _eventDispatcherService.Dispatch(new SuperPinChangedSignal());
            }
            else
            {
                _eventDispatcherService.Dispatch(new PinChangedSignal());
            }

            _isClaimInProgress = false;
            _uiService.HidePopup<BoosterIntroPresenter>();
        }

        private int GetRewardAmount(RemoteConfigModel remoteConfigModel)
        {
            return _popupType == GetHintPopupType.SuperPin
                ? remoteConfigModel.StartingSuperPins
                : remoteConfigModel.StartingPins;
        }
    }
}