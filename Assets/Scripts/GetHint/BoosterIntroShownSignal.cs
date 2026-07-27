using General.EventDispatcher;

namespace GetHint
{
    public class BoosterIntroShownSignal : ISignal
    {
        public readonly GetHintPopupType PopupType;

        public BoosterIntroShownSignal(GetHintPopupType popupType)
        {
            PopupType = popupType;
        }
    }
}