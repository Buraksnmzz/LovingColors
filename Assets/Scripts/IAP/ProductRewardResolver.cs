using GameConfig;

namespace IAP
{
    public static class ProductRewardResolver
    {
        public static CatalogProduct Resolve(RemoteConfigModel configModel, string productId)
        {
            if (configModel == null || string.IsNullOrEmpty(productId))
            {
                return new CatalogProduct { Id = productId };
            }

            if (productId == ProductIds.NoAdsPack)
            {
                return new CatalogProduct
                {
                    Id = productId,
                    Coins = configModel.NoAdsPackCoinReward,
                };
            }

            if (productId == ProductIds.CoinPack1)
            {
                return new CatalogProduct { Id = productId, Coins = configModel.ShopCoinReward1 };
            }
            
            if (productId == ProductIds.CoinPack2)
            {
                return new CatalogProduct { Id = productId, Coins = configModel.ShopCoinReward2 };
            }
            
            if (productId == ProductIds.CoinPack3)
            {
                return new CatalogProduct { Id = productId, Coins = configModel.ShopCoinReward3 };
            }
            
            if (productId == ProductIds.CoinPack4)
            {
                return new CatalogProduct { Id = productId, Coins = configModel.ShopCoinReward4 };
            }
            
            if (productId == ProductIds.CoinPack5)
            {
                return new CatalogProduct { Id = productId, Coins = configModel.ShopCoinReward5 };
            }

            return new CatalogProduct { Id = productId };
        }
    }
}