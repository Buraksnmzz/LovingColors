using General;
using General.EventDispatcher;
using UnityEngine;

public class SafeAreaHelper : MonoBehaviour
{
    public static float VerticalCompensationOffset { get; private set; }

    private static SafeAreaHelper _instance;

    private RectTransform _rectTransform;
    private bool _isBannerVisible;
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
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
        _eventDispatcherService.AddListener<BannerVisibilityChangedSignal>(OnBannerVisibilityChanged);
        _isBannerVisible = false;
        YoogoLabManager.HideBanner();
        Refresh();
    }

    private void OnBannerVisibilityChanged(BannerVisibilityChangedSignal signal)
    {
        _isBannerVisible = signal.Visible;
        if (!_isBannerVisible)
            YoogoLabManager.HideBanner();
        Refresh();
    }

    public static void RefreshForBannerVisibility(bool isBannerVisible)
    {
        if (_instance == null)
            return;

        _instance._isBannerVisible = isBannerVisible;
        if (!isBannerVisible)
            YoogoLabManager.HideBanner();

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