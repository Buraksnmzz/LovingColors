using Collectible;
using Core.Scripts.Services;
using DailyChallenge;
using Gameplay;
using Gameplay.Levels;
using General;
using General.EventDispatcher;
using IAP;
using MainMenu;
using UnityEngine;
using RemoteConfig;
using SavedData;
using Snapshot;
using Sound;
using UI.General;
using UI.Settings;

public class Installer : MonoBehaviour
{
    [Header("UI References")]
    public Transform uiRoot;
    public TextAsset levelConfig;
    public TextAsset dailyChallengeLevelConfig;

    void Awake()
    {
        Vibration.Init();
    }

    private void Start()
    {
        InstallServices();
        SetOptimalFrameRate();
    }

    private void SetOptimalFrameRate()
    {
        var memoryMb = SystemInfo.systemMemorySize;
        var processorCount = SystemInfo.processorCount;

        var isLowEndDevice = memoryMb <= 2100 || processorCount <= 4;

        if (isLowEndDevice)
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }
    }

    private void InstallServices()
    {
        ServiceLocator.Register<IEventDispatcherService>(new EventDispatcherService());
        ServiceLocator.Register<ISoundService>(new SoundService());
        ServiceLocator.Register<IHapticService>(new HapticService());
        ServiceLocator.Register<IUIService>(new UIService(uiRoot));
        ServiceLocator.Register<ISnapshotService>(new SnapshotService());
        ServiceLocator.Register<ICollectibleService>(new CollectibleService());
        ServiceLocator.Register<IIAPService>(new IAPService());
        ServiceLocator.Register<IAdsService>(new AdsService());
        ServiceLocator.Register<ILevelService>(new LevelService(levelConfig, dailyChallengeLevelConfig));
        ServiceLocator.Register<IDailyChallengeService>(new DailyChallengeService());
        var savedDataService = ServiceLocator.GetService<ISavedDataService>();
        if (!savedDataService.GetModel<SettingsModel>().IsNoAds)
            YoogoLabManager.ShowBanner();

        var uiService = ServiceLocator.GetService<IUIService>();
        if (PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 0)
        {
            uiService.ShowPopup<GameplayPresenter>();
        }
        else
        {
            uiService.ShowPopup<HomePresenter>();
        }
    }

}
