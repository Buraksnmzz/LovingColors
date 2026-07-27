using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetHint
{
    public class BoosterIntroView: BaseView
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button getBoosterButton;
        [SerializeField] private Image boosterImage;
        [SerializeField] private Sprite pinSpriteMiddle;
        [SerializeField] private Sprite superPinSpriteMiddle;
        [SerializeField] private TextMeshProUGUI pinAmountText;
        [SerializeField] private TextMeshProUGUI superPinAmountText;
        [SerializeField] private TextMeshProUGUI boosterRewardAmountText;
        [SerializeField] private Transform topBar;
        [SerializeField] private Image flyBoosterImage;
        [SerializeField] private Transform pinImageTarget;
        [SerializeField] private Transform superPinImageTarget;
        [SerializeField] private Sprite flyPinSprite;
        [SerializeField] private Sprite flySuperPinSprite;
        [SerializeField] private float topBarIntroDuration = 0.35f;
        [SerializeField] private float topBarIntroYOffset = 260f;
        [SerializeField] private float boosterFlyDuration = 0.5f;
        
        public event Action GetBoosterButtonClicked;
        
        private Tween _topBarTween;
        private Tween _boosterFlyTween;
        private Vector3 _topBarInitialLocalPosition;
        private Vector3 _boosterImageInitialPosition;
        private GetHintPopupType _popupType = GetHintPopupType.Pin;
        
        private void Start()
        {
            //_topBarInitialLocalPosition = topBar.localPosition;
            _boosterImageInitialPosition = flyBoosterImage.transform.position;
            flyBoosterImage.gameObject.SetActive(false);
            getBoosterButton.onClick.AddListener(() => GetBoosterButtonClicked?.Invoke());
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
        
        private void PlayTopBarIntro()
        {
            //_topBarInitialLocalPosition = topBar.localPosition;
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition, topBarIntroDuration)
                .SetEase(Ease.OutBack);
        }
        
        private void PrepareTopBarIntro()
        {
            _topBarInitialLocalPosition = topBar.localPosition;
            topBar.localPosition = _topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset;
        }
        
        public void SetPinAmount(int value)
        {
            pinAmountText.text = value.ToString();
        }

        public void SetSuperPinAmount(int value)
        {
            superPinAmountText.text = value.ToString();
        }
        
        public void PlayBoosterFly(Action onCompleted)
        {
            var target = ResolveFlyTarget(_popupType);
            flyBoosterImage.sprite = ResolveFlySprite(_popupType);
            

            _boosterImageInitialPosition = flyBoosterImage.transform.position;
            _boosterFlyTween?.Kill();
            flyBoosterImage.gameObject.SetActive(true);
            flyBoosterImage.transform.position = _boosterImageInitialPosition;

            _boosterFlyTween = flyBoosterImage.transform.DOMove(target.position, boosterFlyDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    flyBoosterImage.gameObject.SetActive(false);
                    flyBoosterImage.transform.position = _boosterImageInitialPosition;
                    onCompleted?.Invoke();
                });
        }
        
        private Sprite ResolveFlySprite(GetHintPopupType popupType)
        {
            switch (popupType)
            {
                case GetHintPopupType.Pin:
                    return flyPinSprite;
                default:
                    return flySuperPinSprite;
            }
        }
        private Transform ResolveFlyTarget(GetHintPopupType popupType)
        {
            switch (popupType)
            {
                case GetHintPopupType.Pin:
                    return pinImageTarget;
                default:
                    return superPinImageTarget;
            }
        }
        
        public void ConfigurePopup(GetHintPopupType popupType)
        {
            _popupType = popupType;
            boosterImage.sprite = popupType == GetHintPopupType.Pin ? pinSpriteMiddle : superPinSpriteMiddle;
        }
        
        public override void Hide()
        {
            base.Hide();
            PlayTopBarClose();
        }
        
        private void PlayTopBarClose()
        {
            _topBarInitialLocalPosition = topBar.localPosition;
            _topBarTween?.Kill();
            _topBarTween = topBar.DOLocalMove(_topBarInitialLocalPosition + Vector3.up * topBarIntroYOffset, topBarIntroDuration)
                .SetEase(Ease.InBack).OnComplete(()=> topBar.localPosition = _topBarInitialLocalPosition);
        }
        
    }
}