using Collectible;
using GameConfig;
using Gameplay;
using Gameplay.Levels;
using General;
using General.EventDispatcher;
using Home;
using Level;
using Localization;
using MainMenu;
using RateUs;
using SavedData;
using Services;
using Sound;
using UI.General;
using UI.Shop;
using UI.RateUs;
using UnityEngine;

namespace Win
{
    public class WinPresenter : BasePresenter<WinView>
    {
        private const int BaseBadgeTargetExperience = 100;

        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private ISoundService _soundService;
        private IAdsService _adsService;
        private IEventDispatcherService _eventDispatcherService;
        private ILocalizationService _localizationService;
        private BadgeSpriteConfig _badgeSpriteConfig;
        private int _rewardCoins;
        private bool _isNewBadgeUnlocked;
        private bool _shouldShowRateUsAfterHide;
        private ILevelService _levelService;
        private RemoteConfigModel _remoteConfigModel;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            _remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            _badgeSpriteConfig = Resources.Load<BadgeSpriteConfig>("BadgeSpriteConfig");
            View.NextButtonClicked += OnNextButtonClicked;
            View.ClaimButtonClicked += OnClaimButtonClicked;
            View.ClaimX2ButtonClicked += OnClaimX2ButtonClicked;
            View.Hidden += OnViewHidden;
            View.IntroAnimationFinished += OnIntroAnimationFinished;
            View.NewBadgeAnimationStarted += OnNewBadgeAnimationStarted;
        }

        private void OnNewBadgeAnimationStarted()
        {
            _soundService.PlaySound(ClipName.NewBadge);
        }

        private void OnNextButtonClicked()
        {
            View.CompleteCoinFly();
            _uiService.HidePopup<WinPresenter>();
            if (_savedDataService.GetModel<LevelProgressModel>().CurrentLevelIndex == _remoteConfigModel.DailyChallengeUnlockLevel -1)
            {
                _uiService.ShowPopup<HomePresenter>();
                _uiService.HidePopup<GameplayPresenter>();
            }
            else
            {
                _uiService.ShowPopup<GameplayPresenter>();
            }
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.NextButtonClicked -= OnNextButtonClicked;
                View.ClaimButtonClicked -= OnClaimButtonClicked;
                View.ClaimX2ButtonClicked -= OnClaimX2ButtonClicked;
            }

            base.Cleanup();
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _soundService.PlaySound(ClipName.WinView);
            View.PlayParticles();
            AwardExperienceAndUpdateBadgeProgress();
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevelNumber = levelProgressModel.CurrentLevelIndex + 1;
            var currentDifficultyType = GetDifficultyType(currentLevelNumber);
            var prevDifficultyType = GetDifficultyType(currentLevelNumber - 1);
            var homeText = _localizationService.GetLocalizedString(LocalizationStrings.Home);
            var levelText = _localizationService.GetLocalizedString(LocalizationStrings.Level);
            var claimText = _localizationService.GetLocalizedString(LocalizationStrings.Claim);
            if (_savedDataService.GetModel<LevelProgressModel>().CurrentLevelIndex == _remoteConfigModel.DailyChallengeUnlockLevel - 1)
                View.SetNextButtonText(homeText);
            else
            {
                View.SetNextButtonText(levelText + " " + (levelProgressModel.CurrentLevelIndex + 1));
            }

            View.SetClaim2ButtonText(claimText + " " + "x2");
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            _rewardCoins = _isNewBadgeUnlocked
                ? _remoteConfigModel.NewBadgeRewardCoins
                : GetWinRewardCoins(prevDifficultyType);
            View.SetRewardText(_rewardCoins);
            if (!_isNewBadgeUnlocked)
            {
                collectibleModel.TotalCoins += _rewardCoins;
                _savedDataService.SaveData(collectibleModel);
                _eventDispatcherService.Dispatch(new CoinChangedSignal());
            }

            View.SetCoinFlyAnimatorActive(!_isNewBadgeUnlocked);
            View.SetDifficultyView(currentDifficultyType);
            View.SetCoinCount(_isNewBadgeUnlocked ? collectibleModel.TotalCoins : collectibleModel.TotalCoins - _rewardCoins);
        }

        private LevelDifficultyType GetDifficultyType(int levelNumber)
        {
            if (levelNumber < 1 || !_levelService.TryGetLevelById(levelNumber, out var levelDefinition))
            {
                return LevelDifficultyType.Normal;
            }

            return levelDefinition.Difficulty;
        }

        private void AwardExperienceAndUpdateBadgeProgress()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var previousExperience = collectibleModel.TotalXp;
            var previousBadgeIndex = collectibleModel.CurrentBadgeIndex;

            collectibleModel.TotalXp += Mathf.Max(0, _remoteConfigModel.WinRewardExperience);
            _isNewBadgeUnlocked = false;
            var currentBadgeTargetExperience = GetTargetExperienceForBadgeIndex(collectibleModel.CurrentBadgeIndex);
            while (collectibleModel.TotalXp >= currentBadgeTargetExperience)
            {
                collectibleModel.TotalXp -= currentBadgeTargetExperience;
                collectibleModel.CurrentBadgeIndex++;
                _isNewBadgeUnlocked = true;
                currentBadgeTargetExperience = GetTargetExperienceForBadgeIndex(collectibleModel.CurrentBadgeIndex);
            }

            var targetExperience = currentBadgeTargetExperience;

            _savedDataService.SaveData(collectibleModel);
            var previousBadgeSprite = _badgeSpriteConfig != null
                ? _badgeSpriteConfig.GetBadgeSprite(previousBadgeIndex)
                : null;
            var currentBadgeSprite = _badgeSpriteConfig != null
                ? _badgeSpriteConfig.GetBadgeSprite(collectibleModel.CurrentBadgeIndex)
                : null;

            if (_isNewBadgeUnlocked)
            {
                View.PlayNewBadgeAnimation(previousBadgeSprite, currentBadgeSprite);
            }
            else
            {
                View.PlayWinAnimation(currentBadgeSprite, previousExperience, collectibleModel.TotalXp, targetExperience,
                    collectibleModel.TotalCoins + _remoteConfigModel.WinRewardCoins);
            }
        }

        private int GetTargetExperienceForBadgeIndex(int badgeIndex)
        {
            var safeBadgeIndex = Mathf.Max(0, badgeIndex);
            var threshold = (long)_remoteConfigModel.TargetExperience;

            for (var i = 0; i < safeBadgeIndex; i++)
            {
                threshold *= 2;
                if (threshold >= int.MaxValue)
                    return int.MaxValue;
            }

            return (int)threshold;
        }

        private int GetWinRewardCoins(LevelDifficultyType difficultyType)
        {
            if (difficultyType == LevelDifficultyType.Hard)
            {
                return _remoteConfigModel.WinRewardCoinsHard;
            }

            if (difficultyType == LevelDifficultyType.Extreme)
            {
                return _remoteConfigModel.WinRewardCoinsExtreme;
            }

            return _remoteConfigModel.WinRewardCoins;
        }

        private void OnClaimButtonClicked()
        {
            ClaimReward(_rewardCoins);
        }

        private void OnClaimX2ButtonClicked()
        {
            if (!_adsService.IsRewardedAvailable())
                return;

            View.SetClaimButtonsInteractable(false);
            _adsService.GetReward(OnRewardedCompleted);
        }

        private void OnRewardedCompleted(bool success)
        {
            if (!success)
            {
                View.SetClaimButtonsInteractable(true);
                return;
            }

            ClaimReward(_rewardCoins * 2);
        }

        private void ClaimReward(int amount)
        {
            View.SetClaimButtonsInteractable(false);
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            collectibleModel.TotalCoins += amount;
            _savedDataService.SaveData(collectibleModel);
            _eventDispatcherService.Dispatch(new CoinChangedSignal());

            if (_isNewBadgeUnlocked)
            {
                OnClaimCoinFlyCompleted();
                return;
            }

            View.PlayCoinFly(collectibleModel.TotalCoins, OnClaimCoinFlyCompleted);
        }

        private void OnClaimCoinFlyCompleted()
        {
            OnNextButtonClicked();
        }

        private void OnIntroAnimationFinished()
        {
            if (PlayerPrefs.GetInt(StringConstants.HasRatedGame) == 1)
            {
                _shouldShowRateUsAfterHide = false;
                return;
            }

            var configModel = _savedDataService.GetModel<RemoteConfigModel>();
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var rateTriggerLevels = RateTriggerLevels.FromCommaSeparatedString(configModel.RateTriggerLevels);

            var currentLevel = levelProgressModel.CurrentLevelIndex;
            _shouldShowRateUsAfterHide = false;
            for (var i = 0; i < rateTriggerLevels.TriggerLevels.Length; i++)
            {
                if (rateTriggerLevels.TriggerLevels[i] == currentLevel)
                {
                    _shouldShowRateUsAfterHide = true;
                    break;
                }
            }
        }

        private void OnViewHidden()
        {
            if (!_shouldShowRateUsAfterHide)
            {
                return;
            }

            _shouldShowRateUsAfterHide = false;
#if UNITY_IOS
            YoogoLabManager.ShowNativeReview();
            PlayerPrefs.SetInt(StringConstants.HasRatedGame, 1);
            PlayerPrefs.Save();
            return;
#endif
            _uiService.ShowPopup<RateUsPresenter>();
        }
    }
}