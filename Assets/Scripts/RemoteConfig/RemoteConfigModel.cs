using SavedData;

namespace GameConfig
{
    public class RemoteConfigModel:IModel
    {
        public string RateTriggerLevels = "6,20,50";
        public int ShopCoinReward1 = 200;
        public int ShopCoinReward2 = 200;
        public int ShopCoinReward3 = 200;
        public int ShopCoinReward4 = 200;
        public int ShopCoinReward5 = 200;
        public int ExtraMovesCost = 1500;
        public int ExtraMovesCount = 10;
        public int StartingCoins { get; set; } = 100;
        public int WinRewardCoins { get; set; } = 25;
        public int NoAdsPackCoinReward { get; set; } = 500;
    }
}