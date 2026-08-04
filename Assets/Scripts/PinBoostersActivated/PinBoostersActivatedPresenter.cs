using Collectible;
using GameConfig;
using Level;
using General;
using General.EventDispatcher;
using GetHint;
using SavedData;
using Services;
using UI.General;

namespace PinBoostersActivated
{
    public class PinBoostersActivatedPresenter : BasePresenter<PinBoostersActivatedView>
    {
        private const int BoosterActivationCatchUpLevel = 7;

        private ISavedDataService _savedDataService;
        private IUIService _uiService;
        private IEventDispatcherService _eventDispatcherService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.OkButtonClicked += OnOkButtonClicked;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            GrantCatchUpBoostersIfNeeded();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.OkButtonClicked -= OnOkButtonClicked;
            }

            base.Cleanup();
        }

        private void OnOkButtonClicked()
        {
            _uiService.HidePopup<PinBoostersActivatedPresenter>();
        }

        private void GrantCatchUpBoostersIfNeeded()
        {
            var levelProgressModel = _savedDataService.GetModel<LevelProgressModel>();
            var currentLevel = levelProgressModel.CurrentLevelIndex + 1;
            if (currentLevel <= BoosterActivationCatchUpLevel)
            {
                return;
            }

            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();

            collectibleModel.HasFreePin = true;
            collectibleModel.TotalPins += remoteConfigModel.StartingPins;
            collectibleModel.HasFreeSuperPin = true;
            collectibleModel.TotalSuperPins += remoteConfigModel.StartingSuperPins;

            _savedDataService.SaveData(collectibleModel);
            _eventDispatcherService.Dispatch(new PinChangedSignal());
            _eventDispatcherService.Dispatch(new SuperPinChangedSignal());
        }
    }
}