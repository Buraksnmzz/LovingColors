using System;

namespace General
{
    public class AdsService: IAdsService
    {
        
        public bool IsRewardedAvailable()
        {
            return YoogoLabManager.RewardedAvailable(null, null);
        }
        
        public void GetReward(Action<bool> callback)
        {
            callback?.Invoke(true);
            return;
            YoogoLabManager.RewardedAvailable(
                onAvailable: () =>
                {
                    YoogoLabManager.PlayRewarded(success =>
                    {
                        if (success)
                        {
                            callback?.Invoke(true);
                            return;
                        }
                        callback?.Invoke(false);
                    });
                },
                onUnavailable: () =>
                {
                    callback?.Invoke(false);
                });
        }
    }
}