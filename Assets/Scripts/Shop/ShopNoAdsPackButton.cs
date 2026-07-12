using Collectible;
using General;
using TMPro;
using UI.Settings;
using UI.Shop;
using UnityEngine;

namespace Shop
{
    public class ShopNoAdsPackButton : ShopPurchaseButton
    {
        protected override void GiveReward(bool success)
        {
            if (success)
            {
                var reward = GetProductReward();
                var collectibleModel = SavedDataService.GetModel<CollectibleModel>();
                var settingsModel = SavedDataService.GetModel<SettingsModel>();
                collectibleModel.TotalCoins += reward.Coins;
                settingsModel.IsNoAds = true;
                var shopRewardData = new ShopRewardData
                {
                    CoinReward = reward.Coins,
                    IsNoAds = true,
                    RewardType = RewardType.Pack,
                };
                UIService.ShowPopup<ShopRewardPresenter, ShopRewardData>(shopRewardData);
                SavedDataService.SaveData(collectibleModel);
                SavedDataService.SaveData(settingsModel);
                EventDispatcherService.Dispatch(new RewardGivenSignal(transform));
                EventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(false));
            }
        }
    }
}