using Collectible;
using DailyChallenge;
using DG.Tweening;
using GameConfig;
using Gameplay.Levels;
using Level;
using SavedData;
using General;
using General.EventDispatcher;
using GetHint;
using Home;
using Localization;
using PinBoostersActivated;
using Quit;
using Services;
using Sound;
using SuperPinOffer;
using UI.General;
using UI.Settings;
using UnityEngine;
using Win;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private ILevelService _levelService;
        private IDailyChallengeService _dailyChallengeService;
        private IEventDispatcherService _eventDispatcherService;
        private ISoundService _soundService;
        private IHapticService _hapticService;
        private ILocalizationService _localizationService;
        private SettingsModel _settingsModel;
        private const int PinBoosterIntroLevel = 4;
        private const int SuperPinBoosterIntroLevel = 7;
        private LevelDifficultyType _currentLevelDifficulty;
        private bool _hasUsedSuperPinInCurrentLevel;
        private bool _isWaitingForSuperPinOfferToStartShuffle;
        private Tween _handHintTween;
        private RemoteConfigModel _remoteConfigModel;
        private const int BoosterHandHintCorrectSwapThreshold = 4;
        private GetHintPopupType _pendingBoosterHandHintType;
        private int _correctSwapsAfterBoosterIntroClosed;
        private int _openPopupCount;
        private bool _isWaitingForPinBoostersActivatedToContinueFlow;


        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _levelService = ServiceLocator.GetService<ILevelService>();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _hapticService = ServiceLocator.GetService<IHapticService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            _settingsModel = _savedDataService.GetModel<SettingsModel>();
            _remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
            //View.Shown += OnViewShownCompleted;
            View.Solved += OnViewSolved;
            View.Completed += OnViewCompleted;
            View.MovesChanged += OnViewMovesChanged;
            View.MoveLimitReached += OnViewMoveLimitReached;
            View.ShuffleCompleted += OnViewShuffleCompleted;
            View.CorrectCardPlaced += OnViewCorrectCardPlaced;
            View.DebugLevelStepRequested += OnDebugLevelStepRequested;
            View.BackButtonClicked += OnBackButtonClicked;
            View.HintClicked += OnHintClicked;
            View.PinClicked += OnPinClicked;
            View.SuperPinClicked += OnSuperPinClicked;
            _eventDispatcherService.AddListener<ContinueWithCoinSignal>(OnContinueWithCoinSignal);
            _eventDispatcherService.AddListener<ContinueWithRewardedSignal>(OnContinueWithRewardedSignal);
            _eventDispatcherService.AddListener<RestartButtonClickSignal>(OnRestartButtonClick);
            _eventDispatcherService.AddListener<HintChangedSignal>(OnHintChanged);
            _eventDispatcherService.AddListener<PinChangedSignal>(OnPinChanged);
            _eventDispatcherService.AddListener<SuperPinChangedSignal>(OnSuperPinChanged);
            _eventDispatcherService.AddListener<SuperPinOfferClosedSignal>(OnSuperPinOfferClosed);
            _eventDispatcherService.AddListener<BoosterIntroClosedSignal>(OnBoosterIntroClosed);
            _eventDispatcherService.AddListener<PopupVisibilityChangedSignal>(OnPopupVisibilityChanged);
        }

        private void OnPopupVisibilityChanged(PopupVisibilityChangedSignal signal)
        {
            if (signal.ViewType == typeof(GameplayView))
            {
                return;
            }

            if (signal.IsVisible)
            {
                _openPopupCount++;
                PauseHandHintTimer();
                return;
            }

            _openPopupCount = Mathf.Max(0, _openPopupCount - 1);
            if (_openPopupCount == 0)
            {
                ResumeHandHintTimer();
            }

            if (signal.ViewType == typeof(PinBoostersActivatedView) &&
                !signal.IsVisible &&
                _openPopupCount == 0)
            {
                ContinueSuperPinFlowAfterPinBoostersActivatedClosed();
            }
        }

        private void OnHintClicked()
        {
            HideHandHint();
            KillHandHintTimer();

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            if (collectibleModel.TotalHints <= 0)
            {
                ShowGetHintPopup(GetHintPopupType.Hint);
                return;
            }

            if (!View.UseHint())
            {
                return;
            }

            collectibleModel.TotalHints--;
            View.SetHintAmount(collectibleModel.TotalHints);
            _soundService.PlaySound(ClipName.Booster);
        }

        private void OnPinClicked()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            if (!IsPinUnlockedForCurrentLevel())
            {
                return;
            }

            if (_hasUsedSuperPinInCurrentLevel)
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var hasFreePin = collectibleModel.HasFreePin;
            if (!hasFreePin && collectibleModel.TotalPins <= 0)
            {
                ShowGetHintPopup(GetHintPopupType.Pin);
                return;
            }

            if (!View.UsePin())
            {
                return;
            }

            HideHandHint();
            ClearPendingBoosterHandHint();

            _soundService.PlaySound(ClipName.Booster);
            if (hasFreePin)
            {
                collectibleModel.HasFreePin = false;
            }
            else
            {
                collectibleModel.TotalPins--;
            }

            _savedDataService.SaveData(collectibleModel);
            View.SetFreeBoosterState(collectibleModel.HasFreePin, collectibleModel.HasFreeSuperPin);
            View.SetPinAmount(collectibleModel.TotalPins);
            _eventDispatcherService.Dispatch(new PinChangedSignal());
        }

        private void OnSuperPinClicked()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            if (!IsSuperPinUnlockedForCurrentLevel())
            {
                return;
            }

            if (_hasUsedSuperPinInCurrentLevel)
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var hasFreeSuperPin = collectibleModel.HasFreeSuperPin;
            if (!hasFreeSuperPin && collectibleModel.TotalSuperPins <= 0)
            {
                ShowGetHintPopup(GetHintPopupType.SuperPin);
                return;
            }

            if (!View.UseSuperPin())
            {
                return;
            }

            HideHandHint();
            ClearPendingBoosterHandHint();

            _soundService.PlaySound(ClipName.Booster);
            if (hasFreeSuperPin)
            {
                collectibleModel.HasFreeSuperPin = false;
            }
            else
            {
                collectibleModel.TotalSuperPins--;
            }

            _savedDataService.SaveData(collectibleModel);
            _hasUsedSuperPinInCurrentLevel = true;
            View.SetFreeBoosterState(collectibleModel.HasFreePin, collectibleModel.HasFreeSuperPin);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
            View.SetPinAndSuperPinInteractable(false);
            _eventDispatcherService.Dispatch(new SuperPinChangedSignal());
        }

        private void TrackLevelEnd()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            int levelIndex;
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                levelIndex = _dailyChallengeService.GetPlayedLevelId();
            }
            else
            {
                levelIndex = levelProgressModel.CurrentLevelIndex + 1;
            }

            var mode = _dailyChallengeService.HasActiveDailyChallengeGame
                ? StringConstants.FirebaseModeDaily
                : StringConstants.FirebaseModeNormal;

            YoogoLabManager.LogFirebaseEvent(
                StringConstants.FirebaseParamLevelId, GetLevelId(levelIndex),
                StringConstants.FirebaseParamMode, mode);
        }

        private void TrackLevelStart()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            int levelIndex;
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                levelIndex = _dailyChallengeService.GetPlayedLevelId();
            }
            else
            {
                levelIndex = levelProgressModel.CurrentLevelIndex + 1;
            }

            var levelId = GetLevelId(levelIndex);
            var attempt = GetAttemptString(levelProgressModel.CurrentLevelAttemptCount);
            var mode = _dailyChallengeService.HasActiveDailyChallengeGame
                ? StringConstants.FirebaseModeDaily
                : StringConstants.FirebaseModeNormal;

            YoogoLabManager.LogFirebaseEvent(
                StringConstants.FirebaseEventLevelStart,
                StringConstants.FirebaseParamLevelId, levelId,
                StringConstants.FirebaseParamAttempt, attempt,
                StringConstants.FirebaseParamMode, mode);
        }

        private static string GetLevelId(int levelIndex)
        {
            return $"{StringConstants.FirebaseLevelIdPrefix}{levelIndex:D5}";
        }

        private static string GetAttemptString(int attemptCount)
        {
            return $"{StringConstants.FirebaseAttemptPrefix}{attemptCount}";
        }

        private void IncreaseCurrentLevelAttemptCount()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            levelProgressModel.CurrentLevelAttemptCount++;
            _savedDataService.SaveData(levelProgressModel);
        }

        private void OnRestartButtonClick(RestartButtonClickSignal obj)
        {
            _uiService.HidePopup<DailyChallengeLosePresenter>();
            RestartGame();
        }

        private void OnContinueWithRewardedSignal(ContinueWithRewardedSignal obj)
        {
            HandleAddMoves();
        }

        private void OnContinueWithCoinSignal(ContinueWithCoinSignal obj)
        {
            HandleAddMoves();
        }

        private void OnHintChanged(HintChangedSignal _)
        {
            View.SetHintAmount(_savedDataService.GetModel<CollectibleModel>().TotalHints);
        }

        private void OnSuperPinChanged(SuperPinChangedSignal _)
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            View.SetFreeBoosterState(collectibleModel.HasFreePin, collectibleModel.HasFreeSuperPin);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
        }

        private void OnPinChanged(PinChangedSignal _)
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            View.SetFreeBoosterState(collectibleModel.HasFreePin, collectibleModel.HasFreeSuperPin);
            View.SetPinAmount(collectibleModel.TotalPins);
        }

        private void OnSuperPinOfferClosed(SuperPinOfferClosedSignal signal)
        {
            if (!_isWaitingForSuperPinOfferToStartShuffle)
            {
                return;
            }

            View.SetDebugButtonsInteractable(true);

            if (signal.HasGrantedFreeSuperPin)
            {
                UseFreeSuperPinFromOffer();
            }

            _isWaitingForSuperPinOfferToStartShuffle = false;
            View.StartInitialShuffle();
        }

        private void OnBoosterIntroClosed(BoosterIntroClosedSignal signal)
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();

            if (signal.PopupType == GetHintPopupType.Pin && collectibleModel.HasFreePin)
            {
                _pendingBoosterHandHintType = GetHintPopupType.Pin;
                _correctSwapsAfterBoosterIntroClosed = 0;
                return;
            }

            if (signal.PopupType == GetHintPopupType.SuperPin && collectibleModel.HasFreeSuperPin)
            {
                _pendingBoosterHandHintType = GetHintPopupType.SuperPin;
                _correctSwapsAfterBoosterIntroClosed = 0;
                return;
            }

            ClearPendingBoosterHandHint();
        }

        private void OnViewShuffleCompleted()
        {
            ResetHandHintTimer();
            TryShowBoosterIntroAfterShuffle();
        }

        private void TryShowBoosterIntroAfterShuffle()
        {
            if (IsDefaultSuperPinModeActive() || _dailyChallengeService.HasActiveDailyChallengeGame || HasShownAllBoosterIntros())
            {
                return;
            }

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevel = levelProgressModel.CurrentLevelIndex + 1;

            if (currentLevel == _remoteConfigModel.PinBoosterIntroLevel)
            {
                ShowPinBoosterIntro();
                return;
            }

            if (currentLevel == _remoteConfigModel.SuperPinBoosterIntroLevel)
            {
                ShowSuperPinBoosterIntro();
            }
        }

        private void OnViewCorrectCardPlaced()
        {
            HideHandHint();
            RegisterBoosterHandHintProgress();
            ResetHandHintTimer();
        }

        private void RegisterBoosterHandHintProgress()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            if (_pendingBoosterHandHintType != GetHintPopupType.Pin &&
                _pendingBoosterHandHintType != GetHintPopupType.SuperPin)
            {
                return;
            }

            _correctSwapsAfterBoosterIntroClosed++;
            if (_correctSwapsAfterBoosterIntroClosed < BoosterHandHintCorrectSwapThreshold)
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            if (_pendingBoosterHandHintType == GetHintPopupType.Pin)
            {
                if (!collectibleModel.HasFreePin)
                {
                    ClearPendingBoosterHandHint();
                    return;
                }

                View.ShowBoosterHandHint(false);
                ClearPendingBoosterHandHint();
                return;
            }

            if (!collectibleModel.HasFreeSuperPin)
            {
                ClearPendingBoosterHandHint();
                return;
            }

            View.ShowBoosterHandHint(true);
            ClearPendingBoosterHandHint();
        }

        private void ResetHandHintTimer()
        {
            if (!IsTutorialCompleted())
            {
                HideHandHint();
                KillHandHintTimer();
                return;
            }

            if (HasShownHandHint())
            {
                return;
            }

            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();

            _handHintTween?.Kill();
            _handHintTween = DOVirtual.DelayedCall(remoteConfigModel.HintIntroDelay, OnHandHintTimerCompleted, false);
            if (_openPopupCount > 0)
            {
                _handHintTween.Pause();
            }
        }

        private void OnHandHintTimerCompleted()
        {
            _handHintTween = null;

            if (HasShownHandHint())
            {
                return;
            }

            ShowHandHint();
        }

        private void ShowHandHint()
        {
            if (HasShownHandHint())
            {
                return;
            }

            _settingsModel.HasShownHandHint = true;
            _savedDataService.SaveData(_settingsModel);
            View.ShowHandHint();
        }

        private void HideHandHint()
        {
            View.HideHandHint();
        }

        private void KillHandHintTimer()
        {
            _handHintTween?.Kill();
            _handHintTween = null;
        }

        private void PauseHandHintTimer()
        {
            if (_handHintTween != null && _handHintTween.IsActive())
            {
                _handHintTween.Pause();
            }
        }

        private void ResumeHandHintTimer()
        {
            if (_handHintTween != null && _handHintTween.IsActive())
            {
                _handHintTween.Play();
            }
        }

        private void ShowPinBoosterIntro()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            if (HasShownPinBoosterIntro())
            {
                return;
            }

            _settingsModel.HasShownPinBoosterIntro = true;
            _savedDataService.SaveData(_settingsModel);
            RefreshBoosterUnlockStateForCurrentLevel();
            _uiService.ShowPopup<BoosterIntroPresenter, GetHintPopupData>(new GetHintPopupData(GetHintPopupType.Pin));
        }

        private void ShowSuperPinBoosterIntro()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return;
            }

            if (HasShownSuperPinBoosterIntro())
            {
                return;
            }

            _settingsModel.HasShownSuperPinBoosterIntro = true;
            _savedDataService.SaveData(_settingsModel);
            RefreshBoosterUnlockStateForCurrentLevel();
            _uiService.ShowPopup<BoosterIntroPresenter, GetHintPopupData>(new GetHintPopupData(GetHintPopupType.SuperPin));
        }


        private bool HasShownHandHint()
        {
            return _settingsModel.HasShownHandHint;
        }

        private bool HasShownPinBoosterIntro()
        {
            return _settingsModel.HasShownPinBoosterIntro;
        }

        private bool HasShownSuperPinBoosterIntro()
        {
            return _settingsModel.HasShownSuperPinBoosterIntro;
        }

        private bool HasShownAllBoosterIntros()
        {
            return HasShownPinBoosterIntro() && HasShownSuperPinBoosterIntro();
        }

        private static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 1;
        }

        private void HandleAddMoves()
        {
            var extraMoves = _savedDataService.GetModel<RemoteConfigModel>().ExtraMovesCount;
            _soundService.PlaySound(ClipName.PowerUp);
            _hapticService.HapticLow();
            View.AddExtraMoves(extraMoves);
            _uiService.HidePopup<DailyChallengeLosePresenter>();
        }

        private void OnBackButtonClicked()
        {
            _uiService.ShowPopup<QuitPresenter>();
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _openPopupCount = 0;
            _isWaitingForPinBoostersActivatedToContinueFlow = false;
            _eventDispatcherService.Dispatch(new GameplayVisibilityChangedSignal(true));
            ApplyPinBoostersVisibilityForCurrentMode();
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            View.SetFreeBoosterState(collectibleModel.HasFreePin, collectibleModel.HasFreeSuperPin);
            View.SetHintAmount(collectibleModel.TotalHints);
            View.SetPinAmount(collectibleModel.TotalPins);
            View.SetSuperPinAmount(collectibleModel.TotalSuperPins);
            View.SetLevelInfoImages(_dailyChallengeService.HasActiveDailyChallengeGame);
            if (HasShownHandHint())
            {
                View.HideHandHint();
            }
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                LoadDailyChallengeLevel(false);
                return;
            }

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            LoadLevelAtIndex(levelProgressModel.CurrentLevelIndex, true, false);
            IncreaseCurrentLevelAttemptCount();
            TrackLevelStart();
            ShowSuperPinOfferIfPossible();
        }

        private void ShowSuperPinOfferIfPossible()
        {
            View.SetSpineAnimation(_currentLevelDifficulty);

            if (IsHardOrExtremeLevel())
            {
                View.SetBackButtonInteractable(false);
                View.SetDebugButtonsInteractable(false);
                _isWaitingForSuperPinOfferToStartShuffle = true;
                DOVirtual.DelayedCall(2f, BeginSuperPinFlowAfterEntryDelay);
                return;
            }

            BeginSuperPinFlowAfterEntryDelay();
        }

        private void BeginSuperPinFlowAfterEntryDelay()
        {
            if (TryShowPinBoostersActivatedPopupIfNeeded())
            {
                _isWaitingForPinBoostersActivatedToContinueFlow = true;
                return;
            }

            ContinueSuperPinFlowAfterPinBoostersActivatedClosed();
        }

        private void ContinueSuperPinFlowAfterPinBoostersActivatedClosed()
        {
            _isWaitingForPinBoostersActivatedToContinueFlow = false;

            if (IsDefaultSuperPinModeActive())
            {
                UseFreeSuperPinFromOffer();
                _isWaitingForSuperPinOfferToStartShuffle = false;
                View.SetBackButtonInteractable(true);
                View.SetDebugButtonsInteractable(true);
                View.StartInitialShuffle();
                return;
            }

            if (IsHardOrExtremeLevel())
            {
                View.SetBackButtonInteractable(true);
                var superPinOfferPresenter = _uiService.ShowPopup<SuperPinOfferPresenter>();
                if (superPinOfferPresenter == null)
                {
                    _isWaitingForSuperPinOfferToStartShuffle = false;
                    View.SetDebugButtonsInteractable(true);
                    View.StartInitialShuffle();
                }

                return;
            }

            _isWaitingForSuperPinOfferToStartShuffle = false;
            View.SetDebugButtonsInteractable(true);
            View.StartInitialShuffle();
        }

        public override void ViewHidden()
        {
            base.ViewHidden();
            _isWaitingForPinBoostersActivatedToContinueFlow = false;
            KillHandHintTimer();
            HideHandHint();
            ClearPendingBoosterHandHint();
            _eventDispatcherService.Dispatch(new GameplayVisibilityChangedSignal(false));
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.Solved -= OnViewSolved;
                View.Completed -= OnViewCompleted;
                View.MovesChanged -= OnViewMovesChanged;
                View.MoveLimitReached -= OnViewMoveLimitReached;
                View.ShuffleCompleted -= OnViewShuffleCompleted;
                View.CorrectCardPlaced -= OnViewCorrectCardPlaced;
                View.DebugLevelStepRequested -= OnDebugLevelStepRequested;
                View.BackButtonClicked -= OnBackButtonClicked;
                View.HintClicked -= OnHintClicked;
                View.PinClicked -= OnPinClicked;
                View.SuperPinClicked -= OnSuperPinClicked;
            }

            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<ContinueWithCoinSignal>(OnContinueWithCoinSignal);
                _eventDispatcherService.RemoveListener<ContinueWithRewardedSignal>(OnContinueWithRewardedSignal);
                _eventDispatcherService.RemoveListener<RestartButtonClickSignal>(OnRestartButtonClick);
                _eventDispatcherService.RemoveListener<HintChangedSignal>(OnHintChanged);
                _eventDispatcherService.RemoveListener<PinChangedSignal>(OnPinChanged);
                _eventDispatcherService.RemoveListener<SuperPinChangedSignal>(OnSuperPinChanged);
                _eventDispatcherService.RemoveListener<SuperPinOfferClosedSignal>(OnSuperPinOfferClosed);
                _eventDispatcherService.RemoveListener<BoosterIntroClosedSignal>(OnBoosterIntroClosed);
                _eventDispatcherService.RemoveListener<PopupVisibilityChangedSignal>(OnPopupVisibilityChanged);
            }

            KillHandHintTimer();

            base.Cleanup();
        }

        private void OnDebugLevelStepRequested(int levelStep)
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
                return;

            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var nextLevelIndex = Mathf.Max(0, levelProgressModel.CurrentLevelIndex + levelStep);
            LoadLevelAtIndex(nextLevelIndex, false);
        }

        private void LoadDailyChallengeLevel(bool startInitialShuffleImmediately = true)
        {
            var levelId = _dailyChallengeService.GetPlayedLevelId();
            LevelDefinition levelDefinition;
            while (!_levelService.TryGetDailyChallengeLevelById(levelId, out levelDefinition))
            {
                levelId--;
                if (levelId <= 0)
                    return;
            }

            var diffText = "";
            if (levelDefinition.Difficulty == LevelDifficultyType.Hard)
            {
                diffText = _localizationService.GetLocalizedString(LocalizationStrings.Hard);
            }
            else if (levelDefinition.Difficulty == LevelDifficultyType.Extreme)
            {
                diffText = _localizationService.GetLocalizedString(LocalizationStrings.Extreme);
            }

            _currentLevelDifficulty = levelDefinition.Difficulty;
            View.SetDifficultyText(levelDefinition.Difficulty, diffText);
            View.SetDailyChallengeInfo(true, _dailyChallengeService.GetPlayedDateText());
            View.InitializeBoard(levelDefinition, true, startInitialShuffleImmediately);

            ResetBoosterButtonsForLevel(GetCurrentProgressLevelNumber());

            View.SetSpineAnimation(_currentLevelDifficulty);
            ShowSuperPinOfferIfPossible();


        }

        private void LoadLevelAtIndex(int levelIndex, bool clampToPreviousValidLevel, bool startInitialShuffleImmediately = true)
        {
            View.SetDailyChallengeInfo(false, string.Empty);
            var text = _localizationService.GetLocalizedString(LocalizationStrings.Level);
            View.SetLevelText(text + " " + (levelIndex + 1));
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevelIndex = Mathf.Max(0, levelIndex);
            var currentLevelId = currentLevelIndex + 1;
            LevelDefinition levelDefinition;

            while (!_levelService.TryGetLevelById(currentLevelId, out levelDefinition))
            {
                if (!clampToPreviousValidLevel || currentLevelIndex == 0)
                {
                    return;
                }

                currentLevelIndex--;
                currentLevelId = currentLevelIndex + 1;
            }

            var diffText = "";
            if (levelDefinition.Difficulty == LevelDifficultyType.Hard)
            {
                diffText = _localizationService.GetLocalizedString(LocalizationStrings.Hard);
            }
            else if (levelDefinition.Difficulty == LevelDifficultyType.Extreme)
            {
                diffText = _localizationService.GetLocalizedString(LocalizationStrings.Extreme);
            }

            if (currentLevelIndex != levelProgressModel.CurrentLevelIndex)
            {
                levelProgressModel.CurrentLevelIndex = currentLevelIndex;
                _savedDataService.SaveData(levelProgressModel);
            }
            _currentLevelDifficulty = levelDefinition.Difficulty;
            View.SetDifficultyText(levelDefinition.Difficulty, diffText);
            View.InitializeBoard(levelDefinition, false, startInitialShuffleImmediately);
            ResetBoosterButtonsForLevel(currentLevelId);
            if (ShouldShowFirstLevelTutorial(currentLevelIndex))
            {
                View.StartFirstLevelTutorial();
            }
        }

        private bool ShouldShowFirstLevelTutorial(int levelIndex)
        {
            return levelIndex == 0 && PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 0;
        }

        private void OnViewSolved()
        {
            HideHandHint();
            KillHandHintTimer();
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            levelProgressModel.CurrentLevelIndex++;
            _savedDataService.SaveData(levelProgressModel);
            YoogoLabManager.LevelEnd(levelProgressModel.CurrentLevelIndex);
            TrackLevelEnd();
            View.SetInteractionLocked(true);
        }



        private void OnViewMovesChanged(int moveCount, int totalMoveCount)
        {
            if (!_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                return;
            }

            View.SetMovesText(moveCount, totalMoveCount);
        }

        private void OnViewMoveLimitReached()
        {
            if (!_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                return;
            }

            _uiService.ShowPopup<DailyChallengeLosePresenter>();
        }

        private void RestartGame()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                LoadDailyChallengeLevel(false);
                return;
            }

            // var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            // LoadLevelAtIndex(levelProgressModel.CurrentLevelIndex, true, false);
            // View.SetSpineAnimation(_currentLevelDifficulty);
            // if (_currentLevelDifficulty == LevelDifficultyType.Hard ||
            //     _currentLevelDifficulty == LevelDifficultyType.Extreme)
            // {
            //     _isWaitingForSuperPinOfferToStartShuffle = true;
            //     _uiService.ShowPopup<SuperPinOfferPresenter>();
            //     return;
            // }
            //
            // View.StartInitialShuffle();
        }

        private void OnViewCompleted()
        {
            if (_dailyChallengeService.HasActiveDailyChallengeGame)
            {
                _dailyChallengeService.CompletePlayedDay();
                _uiService.ShowPopup<DailyChallengeWinPresenter>();
                return;
            }

            if (PlayerPrefs.GetInt(StringConstants.IsTutorialShown) == 0)
            {
                PlayerPrefs.SetInt(StringConstants.IsTutorialShown, 1);
                PlayerPrefs.Save();
            }

            _uiService.ShowPopup<WinPresenter>();
        }

        private void ResetBoosterButtonsForLevel(int currentLevel)
        {
            _hasUsedSuperPinInCurrentLevel = false;
            _isWaitingForSuperPinOfferToStartShuffle = false;
            _isWaitingForPinBoostersActivatedToContinueFlow = false;
            ClearPendingBoosterHandHint();
            ApplyPinBoostersVisibilityForCurrentMode();
            View.SetBoosterUnlockState(IsPinUnlockedForLevel(currentLevel), IsSuperPinUnlockedForLevel(currentLevel));
            View.SetPinAndSuperPinInteractable(!IsDefaultSuperPinModeActive());
        }

        private void RefreshBoosterUnlockStateForCurrentLevel()
        {
            var currentLevel = GetCurrentProgressLevelNumber();
            ApplyPinBoostersVisibilityForCurrentMode();
            View.SetBoosterUnlockState(IsPinUnlockedForLevel(currentLevel), IsSuperPinUnlockedForLevel(currentLevel));
            View.SetPinAndSuperPinInteractable(!IsDefaultSuperPinModeActive());
        }

        private int GetCurrentProgressLevelNumber()
        {
            return _savedDataService.GetModel<LevelProgressModel>().CurrentLevelIndex + 1;
        }

        private bool IsPinUnlockedForCurrentLevel()
        {
            return IsPinUnlockedForLevel(GetCurrentProgressLevelNumber());
        }

        private bool IsSuperPinUnlockedForCurrentLevel()
        {
            return IsSuperPinUnlockedForLevel(GetCurrentProgressLevelNumber());
        }

        private bool IsPinUnlockedForLevel(int currentLevel)
        {
            if (IsDefaultSuperPinModeActive())
            {
                return false;
            }

            return currentLevel > PinBoosterIntroLevel || HasShownPinBoosterIntro();
        }

        private bool IsSuperPinUnlockedForLevel(int currentLevel)
        {
            if (IsDefaultSuperPinModeActive())
            {
                return false;
            }

            return currentLevel > SuperPinBoosterIntroLevel || HasShownSuperPinBoosterIntro();
        }

        private void UseFreeSuperPinFromOffer()
        {
            if (_hasUsedSuperPinInCurrentLevel)
            {
                return;
            }

            _hasUsedSuperPinInCurrentLevel = true;
            View.QueueSuperPinActivationAfterShuffle();
            View.SetPinAndSuperPinInteractable(false);
        }

        private void ShowGetHintPopup(GetHintPopupType popupType)
        {
            if (IsDefaultSuperPinModeActive() && popupType != GetHintPopupType.Hint)
            {
                return;
            }

            _uiService.ShowPopup<GetHintPresenter, GetHintPopupData>(new GetHintPopupData(popupType));
        }

        private void ClearPendingBoosterHandHint()
        {
            _pendingBoosterHandHintType = GetHintPopupType.Hint;
            _correctSwapsAfterBoosterIntroClosed = 0;
        }

        private bool IsDefaultSuperPinModeActive()
        {
            return _remoteConfigModel != null && _remoteConfigModel.IsSuperPinActiveInDefault;
        }

        private void ApplyPinBoostersVisibilityForCurrentMode()
        {
            View.SetPinBoostersVisible(!IsDefaultSuperPinModeActive());
        }

        private bool TryShowPinBoostersActivatedPopupIfNeeded()
        {
            if (IsDefaultSuperPinModeActive())
            {
                return false;
            }

            if (!_settingsModel.HasStoredInitialSuperPinActiveInDefault)
            {
                return false;
            }

            if (!_settingsModel.InitialSuperPinActiveInDefault)
            {
                return false;
            }

            if (_settingsModel.HasShownPinBoostersActivatedPopup)
            {
                return false;
            }

            _settingsModel.HasShownPinBoostersActivatedPopup = true;
            _savedDataService.SaveData(_settingsModel);
            _uiService.ShowPopup<PinBoostersActivatedPresenter>();
            return true;
        }

        private bool IsHardOrExtremeLevel()
        {
            return _currentLevelDifficulty == LevelDifficultyType.Hard ||
                   _currentLevelDifficulty == LevelDifficultyType.Extreme;
        }
    }
}
