using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using SavedData;
using UI.Shop;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeReward2Presenter : BasePresenter<DailyChallengeReward2View>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private IAdsService _adsService;
        private IEventDispatcherService _eventDispatcherService;
        private int _rewardCoins;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.ClaimClicked += OnClaimClicked;
            View.ClaimDoubleClicked += OnClaimDoubleClicked;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            _rewardCoins = remoteConfigModel.WinRewardCoins;
            View.SetCoinWonAmount(_rewardCoins);
            View.SetCoinCount(collectibleModel.TotalCoins);
            View.SetButtonsInteractable(true);
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.ClaimClicked -= OnClaimClicked;
                View.ClaimDoubleClicked -= OnClaimDoubleClicked;
            }

            base.Cleanup();
        }

        private void OnClaimClicked()
        {
            ClaimReward(_rewardCoins);
        }

        private void OnClaimDoubleClicked()
        {
            if (!_adsService.IsRewardedAvailable())
                return;

            View.SetButtonsInteractable(false);
            _adsService.GetReward(OnRewardedCompleted);
        }

        private void OnRewardedCompleted(bool success)
        {
            if (!success)
            {
                View.SetButtonsInteractable(true);
                return;
            }

            ClaimReward(_rewardCoins * 2);
        }

        private void ClaimReward(int amount)
        {
            View.SetButtonsInteractable(false);
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            collectibleModel.TotalCoins += amount;
            _savedDataService.SaveData(collectibleModel);
            _eventDispatcherService.Dispatch(new CoinChangedSignal());
            View.PlayCoinAnimation(collectibleModel.TotalCoins, OnCoinFlyCompleted);
        }

        private void OnCoinFlyCompleted()
        {
            _uiService.HidePopup<DailyChallengeReward2Presenter>();
            _uiService.ShowPopup<DailyChallenge.Award.AwardsPresenter>();
        }
    }
}