using SavedData;

namespace GameConfig
{
    public class RemoteConfigModel:IModel
    {
        public string RateTriggerLevels = "6,20,50";
        public int ShopCoinReward1 = 200;
        public int StartingCoins { get; set; } = 100;
        public int WinRewardCoins { get; set; } = 25;
    }
}