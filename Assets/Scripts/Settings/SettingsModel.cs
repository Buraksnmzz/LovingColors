using SavedData;
using UnityEngine;

namespace UI.Settings
{
    public class SettingsModel: IModel
    {
        public bool IsHapticOn { get; set; } = true;
        public bool IsSoundOn { get; set; } = true;
        public bool IsMusicOn { get; set; } = true;
        public bool IsNoAds { get; set; }
        public SystemLanguage CurrentLanguage { get; set; }
    }
}