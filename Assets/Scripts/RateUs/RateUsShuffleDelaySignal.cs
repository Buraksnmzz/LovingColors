using General.EventDispatcher;

namespace RateUs
{
    public class RateUsShuffleDelaySignal : ISignal
    {
        public readonly bool ShouldDelayInitialShuffle;

        public RateUsShuffleDelaySignal(bool shouldDelayInitialShuffle)
        {
            ShouldDelayInitialShuffle = shouldDelayInitialShuffle;
        }
    }
}