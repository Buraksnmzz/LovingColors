using General;
using General.EventDispatcher;
using SavedData;
using UI.Settings;
using UnityEngine;

public class SafeAreaHelper : MonoBehaviour
{
    public static float VerticalCompensationOffset { get; private set; }

    private static SafeAreaHelper _instance;

    private RectTransform _rectTransform;
    private bool _isBannerVisible;
    private bool _isGameplayViewVisible;
    private bool _requestedBannerVisible;
    private IEventDispatcherService _eventDispatcherService;

    private void Awake()
    {
        _instance = this;
        _rectTransform = GetComponent<RectTransform>();
        _isBannerVisible = false;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_eventDispatcherService != null)
        {
            _eventDispatcherService.RemoveListener<BannerVisibilityChangedSignal>(OnBannerVisibilityChanged);
            _eventDispatcherService.RemoveListener<GameplayVisibilityChangedSignal>(OnGameplayVisibilityChanged);
        }

        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
        _eventDispatcherService.AddListener<BannerVisibilityChangedSignal>(OnBannerVisibilityChanged);
        _eventDispatcherService.AddListener<GameplayVisibilityChangedSignal>(OnGameplayVisibilityChanged);
        _isGameplayViewVisible = false;
        _requestedBannerVisible = GetInitialBannerVisibilityRequest();
        ApplyBannerVisibilityState();
    }

    private void OnBannerVisibilityChanged(BannerVisibilityChangedSignal signal)
    {
        Debug.Log("[OnBannerVisibilityChanged listened. isVisible is " + signal.Visible);
        _requestedBannerVisible = signal.Visible;
        ApplyBannerVisibilityState();
    }

    private void OnGameplayVisibilityChanged(GameplayVisibilityChangedSignal signal)
    {
        _isGameplayViewVisible = signal.IsVisible;
        ApplyBannerVisibilityState();
    }

    public static void RefreshForBannerVisibility(bool isBannerVisible)
    {
        if (_instance == null)
            return;

        _instance._requestedBannerVisible = isBannerVisible;
        _instance.ApplyBannerVisibilityState();

        Canvas.ForceUpdateCanvases();
        _instance.Refresh();
        Canvas.ForceUpdateCanvases();
        _instance.UpdateVerticalCompensationOffset();
    }

    private void Refresh()
    {
        var safeArea = Screen.safeArea;
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        var bannerHeightPercent = _isBannerVisible ? YoogoLabManager.GetBannerHeightPercent() : 0f;
        float bannerHeight = screenHeight * bannerHeightPercent;
        anchorMin.y = (safeArea.y + bannerHeight) / screenHeight;
        anchorMin.x /= screenWidth;
        anchorMax.x /= screenWidth;
        anchorMax.y = (safeArea.y + safeArea.height) / screenHeight;
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        UpdateVerticalCompensationOffset();
    }

    private bool ShouldHideBannerOnCurrentSection()
    {
        return !_isGameplayViewVisible;
    }

    private bool GetInitialBannerVisibilityRequest()
    {
        var savedDataService = ServiceLocator.GetService<ISavedDataService>();
        var isNoAds = savedDataService.GetModel<SettingsModel>().IsNoAds;
        return !isNoAds;
    }

    private void ApplyBannerVisibilityState()
    {
        _isBannerVisible = _requestedBannerVisible && !ShouldHideBannerOnCurrentSection();

        if (!_isBannerVisible)
            YoogoLabManager.HideBanner();
        else
            YoogoLabManager.ShowBanner();

        Refresh();
    }

    private void UpdateVerticalCompensationOffset()
    {
        var parentRectTransform = (RectTransform)_rectTransform.parent;
        var parentHeight = parentRectTransform.rect.height;
        var safeAreaHeight = _rectTransform.rect.height;
        var centeredPositionY = ((_rectTransform.anchorMin.y + _rectTransform.anchorMax.y) * 0.5f - 0.5f) * parentHeight +
                                _rectTransform.anchoredPosition.y;
        VerticalCompensationOffset = parentHeight - safeAreaHeight - centeredPositionY;
    }
}