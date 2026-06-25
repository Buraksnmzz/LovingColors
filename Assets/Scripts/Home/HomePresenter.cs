using Collectible;
using General;
using General.EventDispatcher;
using Gameplay.Levels;
using Home;
using IAP;
using Level;
using Localization;
using RemoteConfig;
using SavedData;
using Services;
using UI.General;
using UI.Settings;
using UI.Shop;
using UnityEngine.Localization;

namespace MainMenu
{
    public class HomePresenter : BasePresenter<HomeView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private IEventDispatcherService _eventDispatcherService;
        private IRemoteConfigService _remoteConfigService;
        private ILocalizationService _localizationService;
        private ILevelService _levelService;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _remoteConfigService = ServiceLocator.GetService<IRemoteConfigService>();
            //_localizationService = ServiceLocator.GetService<ILocalizationService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _eventDispatcherService.AddListener<RewardGivenSignal>(OnRewardGiven);
            View.PlayLevelButtonClicked += OnPlayClicked;
            View.RemoveAdsButtonClicked += OnRemoveAdsClicked;
            View.CoinButtonClicked += OnCoinButtonClicked;
        }

        private void OnRewardGiven(RewardGivenSignal obj)
        {
            var settingsModel = _savedDataService.GetModel<SettingsModel>();
            View.SetNoAdsView(settingsModel.IsNoAds);
        }

        private void OnRemoveAdsClicked()
        {
            //_uiService.ShowPopup<ShopPresenter, ShopOpenData>(ShopOpenData.Default);
        }

        private void OnCoinButtonClicked()
        {
            //_uiService.ShowPopup<ShopPresenter, ShopOpenData>(ShopOpenData.CoinOffers);
        }

        private void OnPlayClicked()
        {
        }

        public override void ViewShown()
        {
            base.ViewShown();
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var settingsModel = _savedDataService.GetModel<SettingsModel>();
            _eventDispatcherService.AddListener<CoinChangedSignal>(OnCoinChanged);
            YoogoLabManager.HideBanner();
            SafeAreaHelper.RefreshForBannerVisibility(false);
            _eventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(false));
            var currentLevelNumber = levelProgressModel.CurrentLevelIndex + 1;
            var currentDifficultyType = GetDifficultyType(currentLevelNumber);
            View.SetCoinText(collectibleModel.TotalCoins);
            //var levelText = _localizationService.GetLocalizedString(LocalizationStrings.Level);
            var levelText = "Level";
            View.SetLevelText(levelText + " " + currentLevelNumber);
            View.SetDifficultyView(currentDifficultyType);
            var nextDifficultyTypes = GetNextDifficultyTypes(currentLevelNumber, View.NextFrameCount);
            var previousDifficultyTypes = GetPreviousDifficultyTypes(currentLevelNumber, View.PreviousFrameCount);
            View.SetLevelFrames(
                currentLevelNumber,
                currentDifficultyType,
                nextDifficultyTypes,
                previousDifficultyTypes);
            View.SetFrameContentPositionYOffset(SafeAreaHelper.VerticalCompensationOffset);
            View.SetNoAdsView(settingsModel.IsNoAds);
        }

        private void OnCoinChanged(CoinChangedSignal _)
        {
            View.SetCoinText(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        private LevelDifficultyType[] GetNextDifficultyTypes(int currentLevelNumber, int frameCount)
        {
            var difficultyTypes = new LevelDifficultyType[frameCount];
            for (var index = 0; index < frameCount; index++)
            {
                difficultyTypes[index] = GetDifficultyType(currentLevelNumber + index + 1);
            }

            return difficultyTypes;
        }

        private LevelDifficultyType[] GetPreviousDifficultyTypes(int currentLevelNumber, int frameCount)
        {
            var difficultyTypes = new LevelDifficultyType[frameCount];
            for (var index = 0; index < frameCount; index++)
            {
                var previousLevelNumber = currentLevelNumber - index - 1;
                difficultyTypes[index] = GetDifficultyType(previousLevelNumber);
            }

            return difficultyTypes;
        }

        private LevelDifficultyType GetDifficultyType(int levelNumber)
        {
            if (levelNumber < 1 || !_levelService.TryGetLevelById(levelNumber, out var levelDefinition))
            {
                return LevelDifficultyType.Normal;
            }

            return levelDefinition.Difficulty;
        }
    }
}