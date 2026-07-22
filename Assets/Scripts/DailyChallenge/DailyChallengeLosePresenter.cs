using Collectible;
using GameConfig;
using Gameplay.Levels;
using General;
using General.EventDispatcher;
using Home;
using Localization;
using SavedData;
using Services;
using Sound;
using UI.General;
using UI.Shop;
using UnityEngine.tvOS;

namespace DailyChallenge
{
    public class DailyChallengeLosePresenter : BasePresenter<DailyChallengeLoseView>
    {
        IEventDispatcherService _eventDispatcher;
        ISavedDataService _savedDataService;
        ILevelService _levelService;
        IDailyChallengeService _dailyChallengeService;
        IUIService _uiService;
        ISoundService _soundService;
        IAdsService _adsService;
        ILocalizationService _localizationService;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _eventDispatcher = ServiceLocator.GetService<IEventDispatcherService>();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            //_localizationService = ServiceLocator.GetService<ILocalizationService>();
            View.RestartButtonClicked += OnRestartButtonClick;
            View.AddMovesClicked += OnAddMovesClick;
            View.ContinueButtonClicked += OnContinueClick;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _soundService.PlaySound(ClipName.Lose);
            var gameConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            View.SetExtraMovesCostText(gameConfigModel.ExtraMovesCost);
            View.SetDifficultySprites(GetCurrentDailyChallengeDifficulty());
            //var youCanAddMovesText = _localizationService.GetLocalizedString(LocalizationStrings.YouCanAddXMoves, gameConfigModel.extraGivenMovesCount);
            //var plusXMovesText = _localizationService.GetLocalizedString(LocalizationStrings.PlusExtraMoves, gameConfigModel.extraGivenMovesCount);
            //View.SetYouCanAddMovesText(youCanAddMovesText);
            //View.SetPlusXMovesText(plusXMovesText);
        }

        private LevelDifficultyType GetCurrentDailyChallengeDifficulty()
        {
            if (_dailyChallengeService == null || _levelService == null)
            {
                return LevelDifficultyType.Normal;
            }

            var levelId = _dailyChallengeService.GetPlayedLevelId();
            if (_levelService.TryGetDailyChallengeLevelById(levelId, out var levelDefinition) && levelDefinition != null)
            {
                return levelDefinition.Difficulty;
            }

            return LevelDifficultyType.Normal;
        }

        private void OnContinueClick()
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var gameConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            if (collectibleModel.TotalCoins < gameConfigModel.ExtraMovesCost)
            {
                //_uiService.ShowPopup<ShopPresenter>();
                return;
            }
            collectibleModel.TotalCoins -= gameConfigModel.ExtraMovesCost;
            _savedDataService.SaveData(collectibleModel);
            _eventDispatcher.Dispatch(new CoinChangedSignal());
            _eventDispatcher.Dispatch(new ContinueWithCoinSignal());
            View.Hide();
        }

        private void OnAddMovesClick()
        {
            if (!_adsService.IsRewardedAvailable())
                return;

            _adsService.GetReward(OnAddMovesRewardCompleted);
        }

        private void OnAddMovesRewardCompleted(bool success)
        {
            if (!success)
            {
                return;
            }

            _eventDispatcher.Dispatch(new ContinueWithRewardedSignal());
            View.Hide();
        }

        private void OnRestartButtonClick()
        {
            _eventDispatcher.Dispatch(new RestartButtonClickSignal());
            View.Hide();
        }

    }
}