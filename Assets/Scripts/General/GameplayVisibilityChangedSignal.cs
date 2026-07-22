using General.EventDispatcher;

namespace General
{
    public class GameplayVisibilityChangedSignal : ISignal
    {
        public bool IsVisible;

        public GameplayVisibilityChangedSignal(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}