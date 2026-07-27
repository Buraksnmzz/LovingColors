using General.EventDispatcher;

namespace SuperPinOffer
{
    public class SuperPinOfferClosedSignal : ISignal
    {
        public readonly bool HasGrantedFreeSuperPin;

        public SuperPinOfferClosedSignal(bool hasGrantedFreeSuperPin)
        {
            HasGrantedFreeSuperPin = hasGrantedFreeSuperPin;
        }
    }
}
