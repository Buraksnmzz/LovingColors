using General.EventDispatcher;

namespace Shop
{
    public class ShopRewardClosedSignal : ISignal
    {
        public bool IsNoAdsOnly;

        public ShopRewardClosedSignal(bool isNoAdsOnly)
        {
            IsNoAdsOnly = isNoAdsOnly;
        }
    }
}