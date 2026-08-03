using System;
using General.EventDispatcher;

namespace UI.General
{
    public class PopupVisibilityChangedSignal : ISignal
    {
        public readonly Type ViewType;
        public readonly bool IsVisible;

        public PopupVisibilityChangedSignal(Type viewType, bool isVisible)
        {
            ViewType = viewType;
            IsVisible = isVisible;
        }
    }
}