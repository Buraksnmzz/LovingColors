using SavedData;
using UnityEngine;

namespace UI.Settings
{
    public class SettingsModel : IModel
    {
        public bool IsHapticOn { get; set; } = true;
        public bool IsSoundOn { get; set; } = true;
        public bool IsMusicOn { get; set; } = true;
        public bool IsNoAds { get; set; }
        public bool HasShownPinBoosterIntro { get; set; }
        public bool HasShownSuperPinBoosterIntro { get; set; }
        public bool HasShownHandHint { get; set; }
        public SystemLanguage CurrentLanguage { get; set; } = SystemLanguage.Unknown;
    }
}