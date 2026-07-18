using Collectible;
using GameConfig;
using Gameplay;
using General;
using Level;
using MainMenu;
using SavedData;
using Sound;
using UI.General;
using UnityEngine;

namespace Win
{
    public class WinPresenter : BasePresenter<WinView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private ISoundService _soundService;
        private IAdsService _adsService;
        private BadgeSpriteConfig _badgeSpriteConfig;
        private int _rewardCoins;
        private bool _isNewBadgeUnlocked;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _badgeSpriteConfig = Resources.Load<BadgeSpriteConfig>("BadgeSpriteConfig");
            View.NextButtonClicked += OnNextButtonClicked;
            View.ClaimButtonClicked += OnClaimButtonClicked;
            View.ClaimX2ButtonClicked += OnClaimX2ButtonClicked;
        }

        private void OnNextButtonClicked()
        {
            View.CompleteCoinFly();
            _uiService.HidePopup<GameplayPresenter>(false);
            _uiService.HidePopup<WinPresenter>();
            if (_savedDataService.GetModel<LevelProgressModel>().CurrentLevelIndex == 10)
                _uiService.ShowPopup<HomePresenter>();
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
            if (_savedDataService.GetModel<LevelProgressModel>().CurrentLevelIndex == 10)
                View.SetNextButtonText("Home");
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            _rewardCoins = remoteConfigModel.WinRewardCoins;
            View.SetRewardText(_rewardCoins);
            if (!_isNewBadgeUnlocked)
            {
                collectibleModel.TotalCoins += _rewardCoins;
                _savedDataService.SaveData(collectibleModel);
            }

            View.SetCoinCount(_isNewBadgeUnlocked ? collectibleModel.TotalCoins : collectibleModel.TotalCoins - _rewardCoins);
        }

        private void AwardExperienceAndUpdateBadgeProgress()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            var targetExperience = Mathf.Max(1, remoteConfigModel.TargetExperience);
            var previousExperience = collectibleModel.TotalXp;

            collectibleModel.TotalXp += Mathf.Max(0, remoteConfigModel.WinRewardExperience);
            _isNewBadgeUnlocked = false;
            while (collectibleModel.TotalXp >= targetExperience)
            {
                collectibleModel.TotalXp -= targetExperience;
                collectibleModel.CurrentBadgeIndex++;
                _isNewBadgeUnlocked = true;
            }

            _savedDataService.SaveData(collectibleModel);
            var badgeSprite = _badgeSpriteConfig != null
                ? _badgeSpriteConfig.GetBadgeSprite(collectibleModel.CurrentBadgeIndex)
                : null;

            if (_isNewBadgeUnlocked)
            {
                View.PlayNewBadgeAnimation(badgeSprite);
            }
            else
            {
                View.PlayWinAnimation(badgeSprite, previousExperience, collectibleModel.TotalXp, targetExperience,
                    collectibleModel.TotalCoins + remoteConfigModel.WinRewardCoins);
            }
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
            View.PlayCoinFly(collectibleModel.TotalCoins, OnClaimCoinFlyCompleted);
        }

        private void OnClaimCoinFlyCompleted()
        {
            OnNextButtonClicked();
        }
    }
}