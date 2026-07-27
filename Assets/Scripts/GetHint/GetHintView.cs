using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetHint
{
    public class GetHintView : BaseView
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private GameObject withCoinObject;
        [SerializeField] private GameObject withVideoObject;
        [SerializeField] private Button getHintWithCoinButton;
        [SerializeField] private Button getHintWithVideoButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image middleImageWithCoin;
        [SerializeField] private Image middleImageWithVideo;
        [SerializeField] private Sprite hintSpriteMiddle;
        [SerializeField] private Sprite pinSpriteMiddle;
        [SerializeField] private Sprite superPinSpriteMiddle;
        [SerializeField] private TextMeshProUGUI hintAmountText;
        [SerializeField] private TextMeshProUGUI pinAmountText;
        [SerializeField] private TextMeshProUGUI superPinAmountText;
        [SerializeField] private TextMeshProUGUI coinAmountText;
        [SerializeField] private TextMeshProUGUI boosterCostText;
        [SerializeField] private TextMeshProUGUI boosterRewardAmountText;
        [SerializeField] private Transform topBar;
        [SerializeField] private Transform flyHintImage;
        [SerializeField] private Transform hintImageTarget;
        [SerializeField] private Transform pinImageTarget;
        [SerializeField] private Transform superPinImageTarget;
        [SerializeField] private Sprite flyHintSprite;
        [SerializeField] private Sprite flyPinSprite;
        [SerializeField] private Sprite flySuperPinSprite;
        [SerializeField] private float topBarIntroDuration = 0.35f;
        [SerializeField] private float topBarIntroYOffset = 260f;
        [SerializeField] private float hintFlyDuration = 0.5f;

        public event Action GetHintWithCoinButtonClicked;
        public event Action GetHintWithVideoButtonClicked;

        private Tween _topBarTween;
        private Tween _hintFlyTween;
        private Vector3 _topBarInitialLocalPosition;
        private bool _hasTopBarInitialLocalPosition;
        private Vector3 _hintImageInitialPosition;
        private bool _hasHintImageInitialPosition;
        private Image _flyImage;
        private GetHintPopupType _popupType = GetHintPopupType.Hint;

        private void Start()
        {
            CacheTopBarPosition();
            CacheHintImagePosition();
            _flyImage = flyHintImage != null ? flyHintImage.GetComponent<Image>() : null;
            if (flyHintImage != null)
                flyHintImage.gameObject.SetActive(false);

            closeButton.onClick.AddListener(Hide);
            getHintWithCoinButton.onClick.AddListener(() => GetHintWithCoinButtonClicked?.Invoke());
            getHintWithVideoButton.onClick.AddListener(() => GetHintWithVideoButtonClicked?.Invoke());
        }

        public void SetBoosterCostText(int amount)
        {
            boosterCostText.text = amount.ToString();
        }

        public void SetBoosterRewardAmountText(int amount)
        {
            boosterRewardAmountText.text = "x" + amount;
        }

        public void SetHeaderAndInfoText(string header, string info)
        {
            headerText.text = header;
            infoText.text = info;
        }

        public override void Show()
        {
            PrepareTopBarIntro();
            base.Show();
        }

        protected override void OnShown()
        {
            base.OnShown();
            PlayTopBarIntro();
        }

        public override void Hide()
        {
            base.Hide();
            PlayTopBarClose();
        }

        protected override void OnDestroy()
        {
            _topBarTween?.Kill();
            _hintFlyTween?.Kill();
            base.OnDestroy();
        }

        public void SetHintAmount(int value)
        {
            hintAmountText.text = value.ToString();
        }

        public void SetPinAmount(int value)
        {
            pinAmountText.text = value.ToString();
        }

        public void SetSuperPinAmount(int value)
        {
            superPinAmountText.text = value.ToString();
        }

        public void SetCoinAmount(int value)
        {
            coinAmountText.text = value.ToString();
        }

        public void SetButtonsInteractable(bool isInteractable)
        {
            getHintWithCoinButton.interactable = isInteractable;
            getHintWithVideoButton.interactable = isInteractable;
        }

        public void PlayHintFly(Action onCompleted)
        {
            if (flyHintImage == null)
            {
                onCompleted?.Invoke();
                return;
            }

            var target = ResolveFlyTarget(_popupType);
            if (target == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (_flyImage != null)
            {
                _flyImage.sprite = ResolveFlySprite(_popupType);
            }

            CacheHintImagePosition();
            _hintFlyTween?.Kill();
            flyHintImage.gameObject.SetActive(true);
            flyHintImage.position = _hintImageInitialPosition;

            _hintFlyTween = flyHintImage.DOMove(target.position, hintFlyDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    flyHintImage.gameObject.SetActive(false);
                    flyHintImage.position = _hintImageInitialPosition;
                    onCompleted?.Invoke();
                });
        }

        public void ConfigurePopup(GetHintPopupType popupType)
        {
            _popupType = popupType;
            var isHint = popupType == GetHintPopupType.Hint;
            var isPin = popupType == GetHintPopupType.Pin;
            var isSuperPin = popupType == GetHintPopupType.SuperPin;

            if (isHint)
            {
                middleImageWithCoin.sprite = hintSpriteMiddle;
                middleImageWithVideo.sprite = hintSpriteMiddle;
            }
            else if (isPin)
            {
                middleImageWithCoin.sprite = pinSpriteMiddle;
                middleImageWithVideo.sprite = pinSpriteMiddle;
            }
            else if (isSuperPin)
            {
                middleImageWithCoin.sprite = superPinSpriteMiddle;
                middleImageWithVideo.sprite = superPinSpriteMiddle;
            }

            withVideoObject.SetActive(!isSuperPin);
            withCoinObject.SetActive(true);
        }

        private Transform ResolveFlyTarget(GetHintPopupType popupType)
        {
            switch (popupType)
            {
                case GetHintPopupType.Pin:
                    return pinImageTarget;
                case GetHintPopupType.SuperPin:
                    return superPinImageTarget;
                default:
                    return hintImageTarget;
            }
        }

        private Sprite ResolveFlySprite(GetHintPopupType popupType)
        {
            switch (popupType)
            {
                case GetHintPopupType.Pin:
                    return flyPinSprite;
                case GetHintPopupType.SuperPin:
                    return flySuperPinSprite;
                default:
                    return flyHintSprite;
            }
        }

        private void CacheTopBarPosition()
        {
            if (_hasTopBarInitialLocalPosition || topBar == null)
                return;

            _topBarInitialLocalPosition = topBar.localPosition;
            _hasTopBarInitialLocalPosition = true;
        }

        private void CacheHintImagePosition()
        {
            if (_hasHintImageInitialPosition || flyHintImage == null)
                return;

            _hintImageInitialPosition = flyHintImage.position;
            _hasHintImageInitialPosition = true;
        }

        private void PrepareTopBarIntro()
        {
            if (topBar == null)
                return;

            CacheTopBarPosition();
            topBar.localPosition = _topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset;
        }

        private void PlayTopBarIntro()
        {
            if (topBar == null)
                return;

            CacheTopBarPosition();
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition, topBarIntroDuration)
                .SetEase(Ease.OutBack);
        }

        private void PlayTopBarClose()
        {
            CacheTopBarPosition();
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset, topBarIntroDuration)
                .SetEase(Ease.InBack);
        }
    }
}