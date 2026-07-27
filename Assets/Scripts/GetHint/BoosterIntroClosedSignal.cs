using General.EventDispatcher;

namespace GetHint
{
    public class BoosterIntroClosedSignal : ISignal
    {
        public readonly GetHintPopupType PopupType;

        public BoosterIntroClosedSignal(GetHintPopupType popupType)
        {
            PopupType = popupType;
        }
    }
}