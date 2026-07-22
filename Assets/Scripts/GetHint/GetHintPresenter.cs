using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using SavedData;
using Shop;
using UI.General;
using UI.Shop;

namespace GetHint
{
    public class GetHintPresenter : BasePresenter<GetHintView>
    {
        private IUIService _uiService;
        private ISavedDataService _savedDataService;
        private IAdsService _adsService;
        private IEventDispatcherService _eventDispatcherService;


        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.GetHintWithCoinButtonClicked += OnGetHintWithCoinButtonClicked;
            View.GetHintWithVideoButtonClicked += OnGetHintWithVideoButtonClicked;

        }

        public override void ViewShown()
        {
            base.ViewShown();
            _eventDispatcherService.AddListener<CoinChangedSignal>(OnCoinChanged);
            _eventDispatcherService.AddListener<HintChangedSignal>(OnHintChanged);
            RefreshView();
            View.SetButtonsInteractable(true);
            View.SetHintCostText(_savedDataService.GetModel<RemoteConfigModel>().HintCost);
        }

        public override void ViewHidden()
        {
            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);
                _eventDispatcherService.RemoveListener<HintChangedSignal>(OnHintChanged);
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
            }

            base.Cleanup();
        }

        private void OnCoinChanged(CoinChangedSignal _)
        {
            View.SetCoinAmount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        private void OnHintChanged(HintChangedSignal _)
        {
            View.SetHintAmount(_savedDataService.GetModel<CollectibleModel>().TotalHints);
        }

        private void OnGetHintWithVideoButtonClicked()
        {
            if (!_adsService.IsRewardedAvailable())
                return;

            View.SetButtonsInteractable(false);
            _adsService.GetReward(OnRewardedCompleted);
        }

        private void OnGetHintWithCoinButtonClicked()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            if (collectibleModel.TotalCoins < remoteConfigModel.HintCost)
            {
                _uiService.ShowPopup<GetCoinsPresenter>();
                return;
            }

            View.SetButtonsInteractable(false);
            collectibleModel.TotalCoins -= remoteConfigModel.HintCost;
            GrantHintAndPlayAnimation(3, collectibleModel, coinAmountChanged: true);
        }

        private void OnRewardedCompleted(bool success)
        {
            if (!success)
            {
                View.SetButtonsInteractable(true);
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            GrantHintAndPlayAnimation(1, collectibleModel, coinAmountChanged: false);
        }

        private void GrantHintAndPlayAnimation(int hintAmount, CollectibleModel collectibleModel, bool coinAmountChanged)
        {
            collectibleModel.TotalHints += hintAmount;
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
            View.SetCoinAmount(collectibleModel.TotalCoins);
        }

        private void OnHintFlyCompleted()
        {
            View.SetHintAmount(_savedDataService.GetModel<CollectibleModel>().TotalHints);
            _eventDispatcherService.Dispatch(new HintChangedSignal());
            _uiService.HidePopup<GetHintPresenter>();
        }
    }
}