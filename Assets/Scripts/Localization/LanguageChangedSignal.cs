using General.EventDispatcher;
using UnityEngine;

namespace Localization
{
    public class LanguageChangedSignal : ISignal
    {
        public SystemLanguage Language;

        public LanguageChangedSignal(SystemLanguage language)
        {
            Language = language;
        }
    }
}