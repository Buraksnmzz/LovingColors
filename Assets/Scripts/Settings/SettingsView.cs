using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    public class SettingsView : BaseView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button hapticButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button restorePurchaseButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Sprite buttonOffSprite;
        [SerializeField] private Sprite buttonOnSprite;
        [SerializeField] private GameObject soundImage;
        [SerializeField] private Image soundButtonImage;
        [SerializeField] private GameObject hapticImage;
        [SerializeField] private Image hapticButtonImage;
        [SerializeField] private GameObject musicImage;
        [SerializeField] private Image musicButtonImage;

        public event Action SoundToggled;
        public event Action HapticToggled;
        public event Action MusicToggled;
        public event Action RestorePurchaseClicked;
        public event Action AboutClicked;
        public event Action LanguageClicked;

        private void Start()
        {
            soundButton.onClick.AddListener(OnSoundClicked);
            hapticButton.onClick.AddListener(OnHapticClicked);
            musicButton.onClick.AddListener(OnMusicClicked);
            restorePurchaseButton.onClick.AddListener(OnRestorePurchaseClicked);
            languageButton.onClick.AddListener(OnLanguageClicked);
            aboutButton.onClick.AddListener(() => AboutClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
        }
    
        private void OnLanguageClicked()
        {
            LanguageClicked?.Invoke();
        }

        public void SetNoAdsView(bool isPurchased)
        {
#if UNITY_IOS
        restorePurchaseButton.gameObject.SetActive(true);
#else
            restorePurchaseButton.gameObject.SetActive(false);
#endif
        }

        public void SetRestoreButtonVisibility(bool shouldShow)
        {
            restorePurchaseButton.gameObject.SetActive(shouldShow);
        }

        private void OnRestorePurchaseClicked()
        {
            RestorePurchaseClicked?.Invoke();
        }
    
        private void OnMusicClicked()
        {
            MusicToggled?.Invoke();
        }

        private void OnHapticClicked()
        {
            HapticToggled?.Invoke();
        }

        private void OnSoundClicked()
        {
            SoundToggled?.Invoke();
        }

        public void SetHapticState(bool settingsModelIsHapticOn)
        {
            hapticButtonImage.sprite = settingsModelIsHapticOn ? buttonOnSprite : buttonOffSprite;
            hapticImage.SetActive(!settingsModelIsHapticOn);
        }

        public void SetSoundState(bool settingsModelIsSoundOn)
        {
            soundButtonImage.sprite = settingsModelIsSoundOn ? buttonOnSprite : buttonOffSprite;
            soundImage.SetActive(!settingsModelIsSoundOn);
        }
    
        public void SetMusicState(bool settingsModelIsMusicOn)
        {
            musicButtonImage.sprite = settingsModelIsMusicOn ? buttonOnSprite : buttonOffSprite;
            musicImage.SetActive(!settingsModelIsMusicOn);
        }
    }
}
