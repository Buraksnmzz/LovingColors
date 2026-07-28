using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace IAP
{
    // Central place to keep product id strings used across the project.
    // Replace the placeholder ids below with the real ids from your store/catalog.
    public static class ProductIds
    {
        public const string CoinPack1 = "lovingcolor_coins_t1";
        public const string CoinPack2 = "lovingcolor_coins_t2";
        public const string CoinPack3 = "lovingcolor_coins_t3";
        public const string CoinPack4 = "lovingcolor_coins_t4";
        public const string CoinPack5 = "lovingcolor_coins_t5";
        public const string NoAdsOnly = "lovingcolor_no_ads";
        public const string NoAdsPack = "lovingcolor_pack"; 

        public static readonly Dictionary<string, ProductType> ProductTypeMap = new Dictionary<string, ProductType>
        {
            { CoinPack1, ProductType.Consumable },
            { CoinPack2, ProductType.Consumable },
            { CoinPack3, ProductType.Consumable },
            { CoinPack4, ProductType.Consumable },
            { CoinPack5, ProductType.Consumable },
            { NoAdsPack, ProductType.NonConsumable },
            { NoAdsOnly, ProductType.NonConsumable }
        };
    }
}