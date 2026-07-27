using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using Localization;
using SavedData;
using Services;
using Shop;
using UI.General;
using UI.Shop;

namespace GetHint
{
    public class GetHintPresenter : BasePresenterWithData<GetHintView, GetHintPopupData>
    {
        private IUIService _uiService;
        private ISavedDataService _savedDataService;
        private IAdsService _adsService;
        private IEventDispatcherService _eventDispatcherService;
        private ILocalizationService _localizationService;
        private GetHintPopupType _popupType = GetHintPopupType.Hint;
        private RewardType _pendingRewardType;

        private enum RewardType
        {
            None,
            Hint,
            Pin,
            SuperPin
        }


        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            View.GetHintWithCoinButtonClicked += OnGetHintWithCoinButtonClicked;
            View.GetHintWithVideoButtonClicked += OnGetHintWithVideoButtonClicked;

        }

        public override void ViewShown()
        {
            base.ViewShown();
            _eventDispatcherService.AddListener<CoinChangedSignal>(OnCoinChanged);
            _eventDispatcherService.AddListener<HintChangedSignal>(OnHintChanged);
            _eventDispatcherService.AddListener<PinChangedSignal>(OnPinChanged);
            _eventDispatcherService.AddListener<SuperPinChangedSignal>(OnSuperPinChanged);
            View.ConfigurePopup(_popupType);
            RefreshView();
            View.SetButtonsInteractable(true);
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            switch (_popupType)
            {
                case GetHintPopupType.Hint:
                    View.SetBoosterCostText(remoteConfigModel.HintCost);
                    View.SetBoosterRewardAmountText(remoteConfigModel.HintRewardWithCoin);
                    View.SetHeaderAndInfoText(_localizationService.GetLocalizedString(LocalizationStrings.Hint),
                        _localizationService.GetLocalizedString(LocalizationStrings.HintInfo));
                    break;
                case GetHintPopupType.Pin:
                    View.SetBoosterCostText(remoteConfigModel.PinCost);
                    View.SetBoosterRewardAmountText(remoteConfigModel.PinRewardWithCoin);
                    View.SetHeaderAndInfoText(_localizationService.GetLocalizedString(LocalizationStrings.Pin),
                        _localizationService.GetLocalizedString(LocalizationStrings.PinInfo));
                    break;
                case GetHintPopupType.SuperPin:
                    View.SetBoosterCostText(remoteConfigModel.SuperPinCost);
                    View.SetBoosterRewardAmountText(remoteConfigModel.SuperPinRewardWithCoin);
                    View.SetHeaderAndInfoText(_localizationService.GetLocalizedString(LocalizationStrings.SuperPin),
                        _localizationService.GetLocalizedString(LocalizationStrings.SuperPinInfo));
                    break;
            }
        }

        public override void ViewHidden()
        {
            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);
                _eventDispatcherService.RemoveListener<HintChangedSignal>(OnHintChanged);
                _eventDispatcherService.RemoveListener<PinChangedSignal>(OnPinChanged);
                _eventDispatcherService.RemoveListener<SuperPinChangedSignal>(OnSuperPinChanged);
            }

            base.ViewHidden();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.GetHintWithCoinButtonClicked -= OnGetHintWithCoinButtonClicked;
                View.GetHintWithVideoButtonClicked -= OnGetHintWithVideoButtonClicked;
            }

            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);
                _eventDispatcherService.RemoveListener<HintChangedSignal>(OnHintChanged);
                _eventDispatcherService.RemoveListener<PinChangedSignal>(OnPinChanged);
                _eventDispatcherService.RemoveListener<SuperPinChangedSignal>(OnSuperPinChanged);
            }

            base.Cleanup();
        }

        protected override void OnDataSet()
        {
            base.OnDataSet();
            _popupType = Data.PopupType;
            if (View != null)
            {
                View.ConfigurePopup(_popupType);
            }
        }

        private void OnCoinChanged(CoinChangedSignal _)
        {
            View.SetCoinAmount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        private void OnHintChanged(HintChangedSignal _)
        {
            View.SetHintAmount(_savedDataService.GetModel<CollectibleModel>().TotalHints);
        }

        private void OnPinChanged(PinChangedSignal _)
        {
            View.SetPinAmount(_savedDataService.GetModel<CollectibleModel>().TotalPins);
        }

        private void OnSuperPinChanged(SuperPinChangedSignal _)
        {
            View.SetSuperPinAmount(_savedDataService.GetModel<CollectibleModel>().TotalSuperPins);
        }

        private void OnGetHintWithVideoButtonClicked()
        {
            if (_popupType == GetHintPopupType.SuperPin)
                return;

            if (!_adsService.IsRewardedAvailable())
                return;

            View.SetButtonsInteractable(false);
            _adsService.GetReward(OnRewardedCompleted);
        }

        private void OnGetHintWithCoinButtonClicked()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            var cost = GetCoinCost(remoteConfigModel);
            if (collectibleModel.TotalCoins < cost)
            {
                _uiService.ShowPopup<GetCoinsPresenter>();
                return;
            }

            View.SetButtonsInteractable(false);
            collectibleModel.TotalCoins -= cost;
            var reward = GetCoinRewardAmount(remoteConfigModel);
            GrantRewardAndPlayAnimation(reward, collectibleModel, coinAmountChanged: true);
        }

        private void OnRewardedCompleted(bool success)
        {
            if (!success)
            {
                View.SetButtonsInteractable(true);
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            GrantRewardAndPlayAnimation(1, collectibleModel, coinAmountChanged: false);
        }

        private void GrantRewardAndPlayAnimation(int rewardAmount, CollectibleModel collectibleModel, bool coinAmountChanged)
        {
            if (rewardAmount <= 0)
            {
                View.SetButtonsInteractable(true);
                return;
            }

            switch (_popupType)
            {
                case GetHintPopupType.Pin:
                    collectibleModel.TotalPins += rewardAmount;
                    _pendingRewardType = RewardType.Pin;
                    break;
                case GetHintPopupType.SuperPin:
                    collectibleModel.TotalSuperPins += rewardAmount;
                    _pendingRewardType = RewardType.SuperPin;
                    break;
                default:
                    collectibleModel.TotalHints += rewardAmount;
                    _pendingRewardType = RewardType.Hint;
                    break;
            }

            _savedDataService.SaveData(collectibleModel);
            if (coinAmountChanged)
                _eventDispatcherService.Dispatch(new CoinChangedSignal());
            View.SetCoinAmount(collectibleModel.TotalCoins);
            View.PlayHintFly(OnHintFlyCompleted);
        }

        private void RefreshView()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            View.SetHintAmount(collectibleModel.TotalHints);
            View.SetPinAmount(collectibleModel.TotalPins);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
            View.SetCoinAmount(collectibleModel.TotalCoins);
        }

        private void OnHintFlyCompleted()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            switch (_pendingRewardType)
            {
                case RewardType.Pin:
                    View.SetPinAmount(collectibleModel.TotalPins);
                    _eventDispatcherService.Dispatch(new PinChangedSignal());
                    break;
                case RewardType.SuperPin:
                    View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
                    _eventDispatcherService.Dispatch(new SuperPinChangedSignal());
                    break;
                default:
                    View.SetHintAmount(collectibleModel.TotalHints);
                    _eventDispatcherService.Dispatch(new HintChangedSignal());
                    break;
            }

            _pendingRewardType = RewardType.None;
            _uiService.HidePopup<GetHintPresenter>();
        }

        private int GetCoinCost(RemoteConfigModel remoteConfigModel)
        {
            switch (_popupType)
            {
                case GetHintPopupType.Pin:
                    return remoteConfigModel.PinCost;
                case GetHintPopupType.SuperPin:
                    return remoteConfigModel.SuperPinCost;
                default:
                    return remoteConfigModel.HintCost;
            }
        }

        private int GetCoinRewardAmount(RemoteConfigModel remoteConfigModel)
        {
            switch (_popupType)
            {
                case GetHintPopupType.Pin:
                    return remoteConfigModel.PinRewardWithCoin;
                case GetHintPopupType.SuperPin:
                    return remoteConfigModel.SuperPinRewardWithCoin;
                default:
                    return remoteConfigModel.HintRewardWithCoin;
            }
        }
    }
}