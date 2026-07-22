using General;
using IAP;
using MainMenu;
using SavedData;
using UI.General;
using General.EventDispatcher;
using Settings;
using Sound;

namespace UI.Settings
{
    public class SettingsPresenter : BasePresenter<SettingsView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private SettingsModel _settingsModel;
        private IIAPService _iapService;
        private IEventDispatcherService _eventDispatcherService;
        private ISoundService _soundService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _iapService = ServiceLocator.GetService<IIAPService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            View.AboutClicked += OnAboutClicked;
            View.RestorePurchaseClicked += OnRestorePurchasesClicked;
            View.LanguageClicked += OnLanguageClicked;
            View.SoundToggled += OnSoundToggle;
            View.HapticToggled += OnHapticToggle;
            View.MusicToggled += OnMusicToggle;
        }

        private void OnMusicToggle()
        {
            _settingsModel.IsMusicOn = !_settingsModel.IsMusicOn;
            View.SetMusicState(_settingsModel.IsMusicOn);
            _savedDataService.SaveData(_settingsModel);
        }

        private void OnHapticToggle()
        {
            _settingsModel.IsHapticOn = !_settingsModel.IsHapticOn;
            View.SetHapticState(_settingsModel.IsHapticOn);
            _savedDataService.SaveData(_settingsModel);
        }

        private void OnSoundToggle()
        {
            _settingsModel.IsSoundOn = !_settingsModel.IsSoundOn;
            View.SetSoundState(_settingsModel.IsSoundOn);
            _savedDataService.SaveData(_settingsModel);
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _settingsModel = _savedDataService.GetModel<SettingsModel>();
            YoogoLabManager.HideBanner();
            View.SetHapticState(_settingsModel.IsHapticOn);
            View.SetSoundState(_settingsModel.IsSoundOn);
            View.SetMusicState(_settingsModel.IsMusicOn);
            bool shouldShow;


#if UNITY_IOS
            shouldShow = true;
#else
            shouldShow = false;
#endif

            View.SetRestoreButtonVisibility(shouldShow);
        }

        private void OnLanguageClicked()
        {
            //_uiService.ShowPopup<LanguageSelectPresenter>();
        }

        private void OnAboutClicked()
        {
            _uiService.ShowPopup<AboutPresenter>();
        }

        private void OnRestorePurchasesClicked()
        {
            if (_iapService.IsInitialized)
            {
                _iapService.RestorePurchasesIOS();
            }
        }
    }
}