using Collectible;
using General;
using General.EventDispatcher;
using SavedData;
using Sound;
using UI.General;
using UI.Shop;
using UnityEngine;

namespace Shop
{
    public class ShopPresenter:  BasePresenter<ShopView>
    {
        ISavedDataService _savedDataService;
        IEventDispatcherService _eventDispatcherService;
        ISoundService _soundService;
        IHapticService _hapticService;
        private Transform _buttonTransform;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _eventDispatcherService.AddListener<RewardGivenSignal>(OnRewardGiven);
            _eventDispatcherService.AddListener<ShopRewardClosedSignal>(OnShopRewardClosed);
            _hapticService = ServiceLocator.GetService<IHapticService>();
        }
        
        private void OnShopRewardClosed(ShopRewardClosedSignal shopRewardClosedSignal)
        {
            if (shopRewardClosedSignal.IsNoAdsOnly)
            {
                return;
            }
            
            View.PlayCoinFly(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }
        
        private void OnRewardGiven(RewardGivenSignal rewardGivenSignal)
        {
            _soundService.PlaySound(ClipName.ShopPurchase);
            _hapticService.HapticLow();
        }
        
        public override void ViewShown()
        {
            base.ViewShown();
            View.SetCoinCount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
            YoogoLabManager.HideBanner();
            _eventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(false));
        }
    }
}