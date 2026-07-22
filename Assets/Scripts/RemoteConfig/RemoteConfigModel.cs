using SavedData;

namespace GameConfig
{
    public class RemoteConfigModel:IModel
    {
        public string RateTriggerLevels = "6,20,50";
        public int ShopCoinReward1 = 200;
        public int ShopCoinReward2 = 400;
        public int ShopCoinReward3 = 600;
        public int ShopCoinReward4 = 800;
        public int ShopCoinReward5 = 1000;
        public int ShopCoinVideoReward = 100;
        public int ExtraMovesCost = 250;
        public int ExtraMovesCount = 10;
        public int HintCost = 150;
        public int WinRewardExperience = 20;
        public int TargetExperience = 100;
        public int StartingCoins { get; set; } = 100;
        public int WinRewardCoins { get; set; } = 25;
        public int NoAdsPackCoinReward { get; set; } = 500;
    }
}